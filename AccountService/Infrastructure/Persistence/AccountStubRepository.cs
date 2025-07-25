using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;

namespace AccountService.Infrastructure.Persistence;

public class AccountStubRepository(StubDbContext db) : IAccountRepository
{
    public Task Add(Account account)
    {
        db.Accounts.Add(account);
        return Task.CompletedTask;
    }

    public IEnumerable<Account>? GetAll()
    {
        return db.Accounts;
    }
}