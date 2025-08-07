using AccountService.Features.Accounts;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();


    public List<Account> Accounts2 { get; set; } = new();
    public List<Transaction> Transactions2 { get; set; } = new();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.OwnerId).HasColumnName("owner_id");
            entity.Property(a => a.Type).HasColumnName("type");
            entity.Property(a => a.Currency).HasColumnName("currency");
            entity.Property(a => a.Balance).HasColumnName("balance");
            entity.Property(a => a.InterestRate).HasColumnName("interest_rate");
            entity.Property(a => a.OpenedAt).HasColumnName("opened_at");
            entity.Property(a => a.ClosedAt).HasColumnName("closed_at");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.AccountId).HasColumnName("account_id");
            entity.Property(t => t.CounterpartyAccountId).HasColumnName("counterparty_account_id");
            entity.Property(t => t.Amount).HasColumnName("amount");
            entity.Property(t => t.Currency).HasColumnName("currency");
            entity.Property(t => t.Type).HasColumnName("type");
            entity.Property(t => t.Description).HasColumnName("description");
            entity.Property(t => t.Timestamp).HasColumnName("timestamp");
        });
    }
}