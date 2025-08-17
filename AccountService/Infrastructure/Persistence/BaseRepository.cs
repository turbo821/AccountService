using Npgsql;
using System.Data;
using System.Data.Common;

namespace AccountService.Infrastructure.Persistence;

public abstract class BaseRepository(IDbConnection connection)
{
    public async Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        if (connection is not NpgsqlConnection conn)
            throw new InvalidOperationException("Connection must be NpgsqlConnection");

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        return await conn.BeginTransactionAsync(isolationLevel);
    }

}