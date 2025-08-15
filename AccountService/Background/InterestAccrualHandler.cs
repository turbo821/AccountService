using AccountService.Features.Accounts.Abstractions;
using Npgsql;

namespace AccountService.Background;

public class InterestAccrualHandler(IConfiguration config, 
    IAccountRepository repo, ILogger<InterestAccrualHandler> logger)
{
    private readonly string _connectionString = config.GetConnectionString("DefaultConnection")!;

    public async Task AccrueDailyInterestAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var transaction = await conn.BeginTransactionAsync();
        try
        {
            await repo.AccrueInterestForAllAsync(transaction);
            await transaction.CommitAsync();

            logger.LogInformation("Daily interest accrual completed successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error in interest accrual");
            throw;
        }
    }
}