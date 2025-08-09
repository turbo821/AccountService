using System.Data;

namespace AccountService.Features.Accounts.Abstractions;

public interface IAccountRepository
{
    Task<List<Account>> GetAllAsync(Guid? ownerId);
    Task<Account?> GetByIdAsync(Guid accountId);
    Task<Account?> GetByIdForUpdateAsync(Guid accountId, IDbTransaction transaction);
    Task<Account?> GetByIdWithTransactionsForPeriodAsync(Guid accountId, DateTime? from, DateTime? to);
    Task<int> GetCountByOwnerIdAsync(Guid ownerId);
    Task AddAsync(Account account);
    Task<int> UpdateAsync(Account account);
    Task<int> UpdateBalanceAsync(Account account, IDbTransaction? transaction = null);
    Task<int> UpdateInterestRateAsync(Account account);
    Task<Guid?> SoftDeleteAsync(Guid accountId);

    Task AddTransactionAsync(Transaction transaction, IDbTransaction? dbTransaction = null);
    Task<decimal> AccrueInterestAsync(Guid accountId, IDbTransaction? dbTransaction = null);
    IDbTransaction BeginTransaction();
}