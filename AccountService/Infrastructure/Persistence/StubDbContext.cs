using AccountService.Features.Accounts;

namespace AccountService.Infrastructure.Persistence;

public class StubDbContext
{
    public List<Account> Accounts { get; set; }
    public List<Transaction> Transactions { get; set; }

    public StubDbContext()
    {
        var accounts = new List<Account>
        {
            new()
            {
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Checking,
                Currency = "USD",
                Balance = 1000m
            },
            new()
            {
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Deposit,
                Currency = "RUB",
                Balance = 10000m,
                InterestRate = 0.01m
            },
            new()
            {
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Checking,
                Currency = "RUB",
                Balance = 10000m,
                InterestRate = 0
            }
        };
        var transactions = new List<Transaction>
        {
            new()
            {
                AccountId = accounts[0].Id,
                Amount = 100m,
                Currency = "USD",
                Type = TransactionType.Debit,
                Description = "Initial deposit",
                Timestamp = DateTime.UtcNow
            },
            new()
            {
                AccountId = accounts[1].Id,
                Amount = 50m,
                Currency = "RUS",
                Type = TransactionType.Credit,
                Description = "Initial credit",
                Timestamp = DateTime.UtcNow
            }
        };

        accounts.ForEach(account => account.Transactions.AddRange(transactions.Where(t => t.AccountId == account.Id)));

        Accounts = accounts;
        Transactions = transactions;
    }
}