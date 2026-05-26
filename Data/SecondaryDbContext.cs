using Microsoft.EntityFrameworkCore;
using LawFlow.Models;

namespace LawFlow.Data;

public class SecondaryDbContext : DbContext
{
    public SecondaryDbContext(DbContextOptions<SecondaryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<CriminalRecord> CriminalRecords { get; set; }
    public DbSet<LegalCase> Cases { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
}
