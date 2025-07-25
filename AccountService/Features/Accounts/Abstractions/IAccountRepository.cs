namespace AccountService.Features.Accounts.Abstractions;

public interface IAccountRepository
{
    Task Add(Account account);
    IEnumerable<Account>? GetAll();
}