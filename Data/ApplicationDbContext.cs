using LawFlow.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LawFlow.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<Case> Cases { get; set; } = null!;
        public DbSet<Hearing> Hearings { get; set; } = null!;
        public DbSet<Verdict> Verdicts { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<PoliceReport> PoliceReports { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Global filter to exclude soft‑deleted entities
            builder.Entity<ApplicationUser>().HasQueryFilter(u => !u.IsDeleted);
            builder.Entity<Case>().HasQueryFilter(c => !c.IsDeleted);
            builder.Entity<Document>().HasQueryFilter(d => !d.IsDeleted);
            builder.Entity<Hearing>().HasQueryFilter(h => !h.IsDeleted);
            builder.Entity<PoliceReport>().HasQueryFilter(p => !p.IsDeleted);
            builder.Entity<Verdict>().HasQueryFilter(v => !v.IsDeleted);
            builder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted);
            builder.Entity<ActivityLog>().HasQueryFilter(a => !a.IsDeleted);
            builder.Entity<Message>().HasQueryFilter(m => !m.IsDeleted);

            // Index on Country for fast filtering
            builder.Entity<Case>().HasIndex(c => c.Country).HasDatabaseName("IX_Case_Country");
            builder.Entity<ApplicationUser>().HasIndex(u => u.Country).HasDatabaseName("IX_User_Country");
            // Configure Case relationships with Restrict delete behavior to avoid SQL Server multiple cascade path cycles
            builder.Entity<Case>()
                .HasOne(c => c.Client)
                .WithMany()
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Lawyer)
                .WithMany()
                .HasForeignKey(c => c.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Judge)
                .WithMany()
                .HasForeignKey(c => c.JudgeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Police)
                .WithMany()
                .HasForeignKey(c => c.PoliceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Clerk)
                .WithMany()
                .HasForeignKey(c => c.ClerkId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Verdict>()
                .HasOne(v => v.Case)
                .WithOne(c => c.Verdict)
                .HasForeignKey<Verdict>(v => v.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Verdict>()
                .HasOne(v => v.Judge)
                .WithMany()
                .HasForeignKey(v => v.JudgeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Document>()
                .HasOne(d => d.Case)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PoliceReport>()
                .HasOne(p => p.Case)
                .WithMany(c => c.PoliceReports)
                .HasForeignKey(p => p.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PoliceReport>()
                .HasOne(p => p.Officer)
                .WithMany()
                .HasForeignKey(p => p.OfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.Case)
                .WithMany()
                .HasForeignKey(m => m.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
