using System.Data;
using System.Data.Common;

namespace AccountService.Features.Accounts.Abstractions;

public interface IAccountRepository
{
    Task<List<Account>> GetAllAsync(Guid? ownerId, AccountType? type = null);
    Task<Account?> GetByIdAsync(Guid accountId, IDbTransaction? transaction = null);
    Task<Account?> GetByIdForUpdateAsync(Guid accountId, IDbTransaction transaction);
    Task<Account?> GetByIdWithTransactionsForPeriodAsync(Guid accountId, DateTime? from, DateTime? to);
    Task<int> GetCountByOwnerIdAsync(Guid ownerId);
    Task AddAsync(Account account, IDbTransaction? transaction = null);
    Task<int> UpdateAsync(Account account, IDbTransaction? transaction = null);
    Task<int> UpdateBalanceAsync(Account account, IDbTransaction? transaction = null);
    Task<int> UpdateInterestRateAsync(Account account, IDbTransaction? transaction = null);
    Task<Guid?> SoftDeleteAsync(Guid accountId, IDbTransaction? transaction = null);
    Task AddTransactionAsync(Transaction transaction, IDbTransaction? dbTransaction = null);
    Task<decimal> AccrueInterestByIdAsync(Guid accountId, IDbTransaction? transaction = null);
    Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
    Task UpdateIsFrozen(Guid ownerId, bool isFrozen, IDbTransaction? transaction = null);
}