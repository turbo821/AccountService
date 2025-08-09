using System.Data;

namespace AccountService.Features.Accounts.Abstractions;

public interface IAccountRepository
{
    Task<List<Account>> GetAllAsync(Guid? ownerId);
    Task<Account?> GetByIdAsync(Guid accountId);
    Task<Account?> GetByIdWithTransactionsForPeriodAsync(Guid accountId, DateTime? from, DateTime? to);
    Task<int> GetCountByOwnerIdAsync(Guid ownerId);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task UpdateBalanceAsync(Account account, IDbTransaction? transaction = null);
    Task UpdateInterestRateAsync(Account account);
    Task<Guid?> SoftDeleteAsync(Guid accountId);

    Task AddTransactionAsync(Transaction transaction, IDbTransaction? dbTransaction = null);
    IDbTransaction BeginTransaction();
}