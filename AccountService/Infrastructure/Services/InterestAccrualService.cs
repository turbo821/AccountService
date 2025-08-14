using AccountService.Features.Accounts.Abstractions;
using Npgsql;

namespace AccountService.Infrastructure.Services;

public class InterestAccrualService(IConfiguration config, 
    IAccountRepository repo, ILogger<InterestAccrualService> logger)
    : IInterestAccrualService
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