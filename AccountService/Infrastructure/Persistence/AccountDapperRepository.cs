using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using Dapper;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace AccountService.Infrastructure.Persistence;

public class AccountDapperRepository(IDbConnection connection) : IAccountRepository
{
    public async Task<List<Account>> GetAllAsync(Guid? ownerId, AccountType? type = null)
    {
        const string sql =
            """
                   SELECT * FROM accounts 
                   WHERE closed_at IS NULL
                   AND (@OwnerId IS NULL OR owner_id = @OwnerId)
                   AND (@Type IS NULL OR type = @Type)
            """;

        var accounts = (await connection.QueryAsync<Account>(sql,
            new { OwnerId = ownerId, Type = type })).ToList();

        return accounts;
    }

    public async Task<Account?> GetByIdAsync(Guid id, IDbTransaction? transaction = null)
    {
        const string sql = 
            // ReSharper disable once StringLiteralTypo
            """
                SELECT
                    id,
                    owner_id,
                    type,
                    currency,
                    balance,
                    interest_rate,
                    opened_at,
                    closed_at,
                    xmin::text::bigint AS Version
                FROM accounts 
                WHERE id = @Id AND closed_at IS NULL
            """;

        var account = await connection.QuerySingleOrDefaultAsync<Account>(sql, new { Id = id }, transaction);
        return account;
    }

    public async Task<Account?> GetByIdForUpdateAsync(Guid accountId, IDbTransaction transaction)
    {
        const string sql =
            // ReSharper disable once StringLiteralTypo
            """
               SELECT id, currency, balance, xmin::text::bigint as Version 
                                            FROM accounts 
                                            WHERE id = @Id 
                                            FOR UPDATE
            """;
        return await connection.QuerySingleOrDefaultAsync<Account>(sql, new { Id = accountId }, transaction);
    }

    public async Task<Account?> GetByIdWithTransactionsForPeriodAsync(Guid id, DateTime? from, DateTime? to)
    {
        const string sql = 
            """
               SELECT * FROM accounts 
               WHERE id = @Id AND closed_at IS NULL;
               
               SELECT * FROM transactions 
               WHERE account_id = @Id 
               AND (@From IS NULL OR timestamp >= @From)
               AND (@To IS NULL OR timestamp <= @To)
               ORDER BY timestamp DESC;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("Id", id);
        parameters.Add("From", from, DbType.DateTime2);
        parameters.Add("To", to, DbType.DateTime2);

        await using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var account = await multi.ReadSingleOrDefaultAsync<Account>();
        if (account != null)
        {
            account.Transactions = (await multi.ReadAsync<Transaction>()).ToList();
        }

        return account;
    }
    public async Task<int> GetCountByOwnerIdAsync(Guid ownerId)
    {
        const string sql = "SELECT COUNT(*) FROM accounts WHERE owner_id = @OwnerId AND closed_at IS NULL";

        return await connection.ExecuteScalarAsync<int>(sql, new { OwnerId = ownerId });

    }

    public async Task AddAsync(Account account, IDbTransaction? transaction = null)
    {
            const string insertAccountSql = 
                """
                    INSERT INTO accounts 
                        (id, owner_id, type, currency, balance, interest_rate, opened_at)
                    VALUES 
                        (@Id, @OwnerId, @Type, @Currency, @Balance, @InterestRate, @OpenedAt)
                """;

            await connection.ExecuteAsync(insertAccountSql, new
            {
                account.Id,
                account.OwnerId,
                Type = (int)account.Type,
                account.Currency,
                account.Balance,
                account.InterestRate,
                account.OpenedAt
            }, transaction);
    }

    public async Task<int> UpdateAsync(Account account, IDbTransaction? transaction = null)
    {
        const string sql =
            // ReSharper disable once StringLiteralTypo
            """
               UPDATE accounts SET
                   owner_id=@OwnerId,
                   type = @Type,
                   currency = @Currency,
                   balance = @Balance,
                   interest_rate = @InterestRate,
                   opened_at=@OpenedAt
               WHERE id = @Id AND closed_at IS NULL 
               AND xmin::text::bigint = @Version
            """;

        return await connection.ExecuteAsync(sql, new
        {
            account.Id,
            account.OwnerId,
            Type = (int)account.Type,
            account.Currency,
            account.Balance,
            account.InterestRate,
            account.OpenedAt,
            account.Version
        }, transaction);
    }

    public async Task<int> UpdateBalanceAsync(Account account, IDbTransaction? transaction = null)
    {
        const string sql =
            // ReSharper disable once StringLiteralTypo
            """
              UPDATE accounts SET
                  balance = @Balance
              WHERE id = @Id AND closed_at IS NULL
              AND xmin::text::bigint = @Version
            """;

        return await connection.ExecuteAsync(sql, new
        {
            account.Id,
            account.Balance,
            account.Version
        }, transaction);
    }

    public async Task<int> UpdateInterestRateAsync(Account account, IDbTransaction? transaction = null)
    {
        const string sql =
            // ReSharper disable once StringLiteralTypo
            """
               UPDATE accounts SET
                   interest_rate = @InterestRate
               WHERE id = @Id AND closed_at IS NULL
               AND xmin::text::bigint = @Version
            """;

        return await connection.ExecuteAsync(sql, new
        {
            account.Id,
            account.InterestRate,
            account.Version
        }, transaction);
    }

    public async Task<Guid?> SoftDeleteAsync(Guid accountId, IDbTransaction? transaction = null)
    {
        const string sql = 
            """
               UPDATE accounts SET
                   closed_at = @ClosedAt
               WHERE id = @Id AND closed_at IS NULL
               RETURNING id
            """;

        return await connection.ExecuteScalarAsync<Guid?>(sql, new
        {
            Id = accountId,
            ClosedAt = DateTime.UtcNow
        }, transaction);
    }

    public async Task AddTransactionAsync(Transaction transaction, IDbTransaction? dbTransaction = null)
    {
        const string sql = 
           """
               INSERT INTO transactions 
                   (id, account_id, counterparty_account_id, amount, currency, type, description, timestamp)
               VALUES 
                   (@Id, @AccountId, @CounterpartyAccountId, @Amount, @Currency, @Type, @Description, @Timestamp)
           """;

        await connection.ExecuteAsync(sql, new
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

    public async Task<decimal> AccrueInterestByIdAsync(Guid accountId, IDbTransaction? transaction = null)
    {
        const string sql = "SELECT accrue_interest(@AccountId);";

        var result = await connection.ExecuteScalarAsync<decimal>(
            sql,
            new { AccountId = accountId },
            transaction: transaction
        );

        return result;
    }

    public async Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        if (connection is not NpgsqlConnection conn)
            throw new InvalidOperationException("Connection must be NpgsqlConnection");

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        return await conn.BeginTransactionAsync(isolationLevel);
    }
}