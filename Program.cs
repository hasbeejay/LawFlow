using LawFlow.Components;
using LawFlow.Data;
using LawFlow.Models;
using LawFlow.Authentication;
using LawFlow.Services;
using LawFlow.Hubs;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor Services
builder.Services.AddMudServices();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        
        // Auto-redirect direct IPv6 host to IPv4 session pooler for network compatibility
        if (host.Equals("db.yxynxedzgthcovccxrci.supabase.co", StringComparison.OrdinalIgnoreCase))
        {
            host = "aws-1-ap-northeast-1.pooler.supabase.com";
            port = 5432;
            username = "postgres.yxynxedzgthcovccxrci";
        }
        
        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true;";
    }
    catch
    {
        // Fallback to original
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()),
    ServiceLifetime.Transient,
    ServiceLifetime.Transient);

// Secondary database context for accounts, cases, chat, etc.
builder.Services.AddDbContext<SecondaryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SecondaryConnection"), npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()),
    ServiceLifetime.Transient,
    ServiceLifetime.Transient);

// Add Identity core
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure Authentication & Authorization
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>(sp => 
    (CustomAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

// Register Application Services
builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<CaseService>();
builder.Services.AddTransient<HearingService>();
builder.Services.AddTransient<DocumentService>();
builder.Services.AddTransient<PoliceReportService>();
builder.Services.AddTransient<VerdictService>();
builder.Services.AddTransient<AuditLogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddTransient<DashboardService>();
builder.Services.AddTransient<MessageService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ToastService>();

// Add SignalR support
builder.Services.AddSignalR();

// Add Razor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.WebHost.ConfigureKestrel(options =>
{
    // Listen on a random free port assigned by the OS to prevent "address already in use"
    options.ListenLocalhost(0);
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Hub
app.MapHub<NotificationHub>("/notificationhub");
app.MapHub<CaseChatHub>("/casechathub");

// Auto Migrations & Seeding during launch
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Read optional flag to force full DB reset (useful for upgrades)
        var forceReset = builder.Configuration.GetValue<bool>("ForceResetDatabase");
        if (forceReset)
        {
            // NOTE: Dropping the public schema is not permitted on Supabase managed databases.
            // The operation is therefore skipped. If you need to reset the database, do it manually
            // via Supabase dashboard or a dedicated migration script.
            // context.Database.ExecuteSqlRaw("DROP SCHEMA IF EXISTS public CASCADE; CREATE SCHEMA public;");
        }
        context.Database.Migrate();

        // Safety net: an earlier `AddMessageChannel` migration was generated empty
        // (the EF tooling ran against a stale build), so existing databases have
        // the migration row recorded but are missing the column. Add it idempotently
        // so chat queries don't break. Safe on fresh DBs because the migration's
        // own Up() also uses IF NOT EXISTS.
        try
        {
            context.Database.ExecuteSqlRaw(
                @"ALTER TABLE ""Messages"" ADD COLUMN IF NOT EXISTS ""Channel"" integer NOT NULL DEFAULT 0;"
            );
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Could not ensure Messages.Channel column exists.");
        }

        // Important for real databases (e.g., Supabase): keep demo seeding opt-in only.
        var shouldSeedDemoData = builder.Configuration.GetValue<bool>("SeedDemoData");
        if (shouldSeedDemoData)
        {
            var authService = services.GetRequiredService<AuthService>();
            await authService.SeedDemoDataAsync();

            var caseService = services.GetRequiredService<CaseService>();
            await caseService.SeedDemoCasesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();
