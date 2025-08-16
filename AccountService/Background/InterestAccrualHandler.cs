using AccountService.Application.Abstractions;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Features.Accounts.Contracts;

namespace AccountService.Background;

public class InterestAccrualHandler(IAccountRepository repo,
    IOutboxRepository outboxRepo, ILogger<InterestAccrualHandler> logger)
{
    public async Task AccrueDailyInterestAsync()
    {
        var accounts = await repo.GetAllAsync(null, AccountType.Deposit);
        foreach (var account in accounts)
        {
            await using var transaction = await repo.BeginTransactionAsync();
            try
            {
                var interestAmount = await repo.AccrueInterestByIdAsync(account.Id, transaction);

                var interestAccrued = new InterestAccrued(Guid.NewGuid(), DateTime.UtcNow, account.Id,
                    DateTime.Now.AddDays(-1), DateTime.Now, interestAmount);
                await outboxRepo.AddAsync(interestAccrued, "account.events", "money.credited", transaction);

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
}