namespace AccountService.Features.Accounts.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id);
    Task<List<Account>> GetAllAsync();
    Task<List<Account>> GetByOwnerIdAsync(Guid ownerId);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task<Guid?> SoftDeleteAsync(Account account);
    Task<bool> ExistsAsync(Guid id);
}