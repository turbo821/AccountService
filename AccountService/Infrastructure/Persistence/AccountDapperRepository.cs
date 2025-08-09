using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using Dapper;
using System.Data;

namespace AccountService.Infrastructure.Persistence;

public class AccountDapperRepository(IDbConnection db, 
    ILogger<AccountDapperRepository> logger) : IAccountRepository
{
    public async Task<List<Account>> GetAllAsync(Guid? ownerId)
    {
        const string sql = """

                                   SELECT * FROM accounts 
                                   WHERE closed_at IS NULL
                                   AND (@OwnerId IS NULL OR owner_id = @OwnerId)
                           """;

        try
        {
            var accounts = (await db.QueryAsync<Account>(sql, new { OwnerId = ownerId })).ToList();
            return accounts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all accounts for owner {OwnerId}", ownerId);
            throw;
        }
    }

    public async Task<Account?> GetByIdAsync(Guid id)
    {
        const string sql = """

                                   SELECT * FROM accounts 
                                   WHERE id = @Id AND closed_at IS NULL
                           """;

        try
        {
            var account = await db.QuerySingleOrDefaultAsync<Account>(sql, new { Id = id });
            return account;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting account by id {Id}", id);
            throw;
        }
    }


    public async Task<Account?> GetByIdWithTransactionsForPeriodAsync(Guid id, DateTime? from, DateTime? to)
    {
        const string sql = """

                                   SELECT * FROM accounts 
                                   WHERE id = @Id AND closed_at IS NULL;
                                   
                                   SELECT * FROM transactions 
                                   WHERE account_id = @Id 
                                   AND (@From IS NULL OR timestamp >= @From)
                                   AND (@To IS NULL OR timestamp <= @To)
                                   ORDER BY timestamp DESC;
                           """;

        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("From", from, DbType.DateTime2);
            parameters.Add("To", to, DbType.DateTime2);

            await using var multi = await db.QueryMultipleAsync(sql, parameters);

            var account = await multi.ReadSingleOrDefaultAsync<Account>();
            if (account != null)
            {
                account.Transactions = (await multi.ReadAsync<Transaction>()).ToList();
            }

            return account;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting account with transactions for period {Id}", id);
            throw;
        }
    }
    public async Task<int> GetCountByOwnerIdAsync(Guid ownerId)
    {
        const string sql = "SELECT COUNT(*) FROM accounts WHERE owner_id = @OwnerId AND closed_at IS NULL";

        try
        {
            return await db.ExecuteScalarAsync<int>(sql, new { OwnerId = ownerId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting account count for owner {OwnerId}", ownerId);
            throw;
        }
    }

    public async Task AddAsync(Account account)
    {
        const string sql = """

                                       INSERT INTO accounts 
                                           (id, owner_id, type, currency, balance, interest_rate, opened_at)
                                       VALUES 
                                           (@Id, @OwnerId, @Type, @Currency, @Balance, @InterestRate, @OpenedAt)
                           """;

        try
        {
            await db.ExecuteAsync(sql, new
            {
                account.Id,
                account.OwnerId,
                Type = (int)account.Type,
                account.Currency,
                account.Balance,
                account.InterestRate,
                account.OpenedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Account account)
    {
        const string sql = """

                                       UPDATE accounts SET
                                           owner_id=@OwnerId,
                                           type = @Type,
                                           currency = @Currency,
                                           balance = @Balance,
                                           interest_rate = @InterestRate,
                                           opened_at=@OpenedAt
                                       WHERE id = @Id AND closed_at IS NULL
                           """;

        try
        {
            await db.ExecuteAsync(sql, new
            {
                account.Id,
                account.OwnerId,
                Type = (int)account.Type,
                account.Currency,
                account.Balance,
                account.InterestRate,
                account.OpenedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task UpdateBalanceAsync(Account account, IDbTransaction? transaction = null)
    {
        const string sql = """

                                       UPDATE accounts SET
                                           balance = @Balance
                                       WHERE id = @Id AND closed_at IS NULL
                           """;

        try
        {
            await db.ExecuteAsync(sql, new
            {
                account.Id,
                account.Balance
            }, transaction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating balance for account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task UpdateInterestRateAsync(Account account)
    {
        const string sql = """

                                       UPDATE accounts SET
                                           interest_rate = @InterestRate
                                       WHERE id = @Id AND closed_at IS NULL
                           """;

        try
        {
            await db.ExecuteAsync(sql, new
            {
                account.Id,
                account.InterestRate
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating interest rate for account {AccountId}", account.Id);
            throw;
        }
    }

    public async Task<Guid?> SoftDeleteAsync(Guid accountId)
    {
        const string sql = """

                                       UPDATE accounts SET
                                           closed_at = @ClosedAt
                                       WHERE id = @Id AND closed_at IS NULL
                                       RETURNING id
                           """;

        try
        {
            return await db.ExecuteScalarAsync<Guid?>(sql, new
            {
                Id = accountId,
                ClosedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error soft deleting account {AccountId}", accountId);
            throw;
        }
    }

    public async Task AddTransactionAsync(Transaction transaction, IDbTransaction? dbTransaction = null)
    {
        const string sql = """

                                       INSERT INTO transactions 
                                           (id, account_id, counterparty_account_id, amount, currency, type, description, timestamp)
                                       VALUES 
                                           (@Id, @AccountId, @CounterpartyAccountId, @Amount, @Currency, @Type, @Description, @Timestamp)
                           """;

        try
        {
            await db.ExecuteAsync(sql, new
            {
                transaction.Id,
                transaction.AccountId,
                transaction.CounterpartyAccountId,
                transaction.Amount,
                transaction.Currency,
                Type = (int)transaction.Type,
                transaction.Description,
                transaction.Timestamp
            }, dbTransaction);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding transaction {TransactionId}", transaction.Id);
            throw;
        }
    }

    public IDbTransaction BeginTransaction()
    {
        if (db.State != ConnectionState.Open)
            db.Open();

        return db.BeginTransaction();
    }
}