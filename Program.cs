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
        
        // Auto-redirect any Supabase host to the IPv4 pooler for network compatibility
if (host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase))
{
    host = "aws-1-ap-northeast-1.pooler.supabase.com";
    port = 5432;
    // Extract project ID from the original host (e.g., db.<project>.supabase.co)
    var hostParts = uri.Host.Split('.');
    string projectId = hostParts.Length >= 3 ? hostParts[1] : "";
    username = $"postgres.{projectId}";
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
    options.Listen(System.Net.IPAddress.Loopback, 0);
});

var app = builder.Build();
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
    var logger = services.GetRequiredService<ILogger<Program>>();
    var dbAvailable = true;

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

        // Ensure the database schema is created based on the current models.
        context.Database.EnsureCreated();

        // Ensure ActivityLogs table exists (in case migrations missed it)
        try
        {
            context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""ActivityLogs"" (""Id"" text NOT NULL, ""UserId"" text NOT NULL, ""Action"" text NOT NULL, ""Details"" text, ""CreatedAt"" timestamp NOT NULL, ""IsDeleted"" boolean NOT NULL DEFAULT FALSE, ""UpdatedAt"" timestamp, CONSTRAINT ""PK_ActivityLogs"" PRIMARY KEY (""Id""));");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create ActivityLogs table via raw SQL.");
        }

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
            logger.LogWarning(ex, "Could not ensure Messages.Channel column exists.");
        }

        // Remove any records that are not from Pakistan to keep data relevant
        try
        {
            context.Database.ExecuteSqlRaw("DELETE FROM \"Cases\" WHERE \"Country\" <> 'Pakistan';");
            context.Database.ExecuteSqlRaw("DELETE FROM \"AspNetUsers\" WHERE \"Country\" <> 'Pakistan';");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not prune country-specific data.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database initialization failed. Continuing startup without DB schema creation.");
        dbAvailable = false;
    }

    if (!dbAvailable)
    {
        logger.LogWarning("Database is not available. The app will continue startup, but identity and data features may fail until a valid database connection is provided.");
    }

    if (dbAvailable)
    {
        try
        {
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
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }
}

app.Run();
