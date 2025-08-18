using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using AccountService.Features.Accounts.Contracts;
using AccountService.Features.Accounts.Abstractions;

namespace AccountService.Infrastructure.Consumers
{
    public class AntifraudConsumer(
        IAccountRepository accRepo,
        IInboxRepository inboxRepo,
        ILogger<AntifraudConsumer> logger)
        : IConsumerHandler
    {
        public async Task HandleAsync(DomainEvent @event, string eventType)
        {
            var isProcessed = await inboxRepo.IsProcessedAsync(@event.EventId, nameof(AntifraudConsumer));
            if (isProcessed)
            {
                logger.LogInformation("Event {EventId} already processed by {ConsumerName}, skipping", @event.EventId, nameof(AntifraudConsumer));
                return;
            }

            await using var transaction = await accRepo.BeginTransactionAsync();
            try
            {
                switch (@event)
                {
                    case ClientBlocked blocked:
                        await accRepo.UpdateIsFrozen(blocked.ClientId, true, transaction);
                        logger.LogInformation("ClientBlocked processed for client {ClientId}", blocked.ClientId);
                        break;

                    case ClientUnblocked unblocked:
                        await accRepo.UpdateIsFrozen(unblocked.ClientId, false, transaction);
                        logger.LogInformation("ClientUnblocked processed for client {ClientId}", unblocked.ClientId);
                        break;
                    default:
                        logger.LogWarning("Unknown event eventType: {Type}", @event.GetType().Name);
                        break;
                }
                await inboxRepo.MarkAsProcessedAsync(@event.EventId, nameof(AntifraudConsumer), transaction);

                await transaction.CommitAsync();
                logger.LogInformation("Antifraud event {EventId} processed by {ConsumerName}", @event.EventId, nameof(AntifraudConsumer));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Error processing event {EventId}", @event.EventId);
                throw;
            }
        }
    }
}
