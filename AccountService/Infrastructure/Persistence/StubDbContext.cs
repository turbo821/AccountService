using AccountService.Features.Accounts;

namespace AccountService.Infrastructure.Persistence;

public class StubDbContext
{
    public List<Account> Accounts { get; set; }
    public List<Transaction> Transactions { get; set; }

    public StubDbContext()
    {
        var accounts = new List<Account>()
        {
            new Account
            {
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Checking,
                Currency = "USD",
                Balance = 1000m,
                OpenedAt = DateTime.UtcNow
            },
            new Account
            {
                OwnerId = Guid.NewGuid(),
                Type = AccountType.Deposit,
                Currency = "RUS",
                Balance = 10000m,
                InterestRate = 0.01m,
                OpenedAt = DateTime.UtcNow
            }
        };
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                AccountId = accounts[0].Id,
                Amount = 100m,
                Currency = "USD",
                Type = TransactionType.Deposit,
                Description = "Initial deposit",
                Timestamp = DateTime.UtcNow
            },
            new Transaction
            {
                AccountId = accounts[1].Id,
                Amount = 50m,
                Currency = "RUS",
                Type = TransactionType.Deposit,
                Description = "Initial deposit",
                Timestamp = DateTime.UtcNow
            }
        };

        accounts.ForEach(account => account.Transactions.AddRange(transactions.Where(t => t.AccountId == account.Id)));

        Accounts = accounts;
        Transactions = transactions;
    }
}