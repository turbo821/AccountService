using AccountService.Features.Accounts;
using AccountService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Extensions;

public static class WebApplicationExtensions
{
    public static async Task DatabaseInitializeAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        if (await db.Accounts.AnyAsync())
            return;

        var accounts = new List<Account>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Checking,
                Currency = "USD",
                Balance = 1000m,
                OpenedAt = new DateTime(2025, 2, 20)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Deposit,
                Currency = "RUB",
                Balance = 10000m,
                InterestRate = 0.01m,
                OpenedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Checking,
                Currency = "RUB",
                Balance = 10000m,
                InterestRate = 0,
                OpenedAt = new DateTime(2021, 10, 5)
            }
        };

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = accounts[0].Id,
                Amount = 100m,
                Currency = "USD",
                Type = TransactionType.Debit,
                Description = "Initial deposit",
                Timestamp = new DateTime(2025, 4, 22)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = accounts[1].Id,
                Amount = 50m,
                Currency = "RUB",
                Type = TransactionType.Credit,
                Description = "Initial credit",
                Timestamp = DateTime.UtcNow
            }
        };

        //accounts.ForEach(account =>
        //{
        //    var related = transactions.Where(t => t.AccountId == account.Id).ToList();
        //    account.Transactions2.AddRange(related);
        //});

        db.Accounts.AddRange(accounts);
        db.Transactions.AddRange(transactions);

        await db.SaveChangesAsync();
    }
}