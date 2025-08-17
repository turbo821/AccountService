using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using AccountService.Features.Accounts.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using AccountService.Features.Accounts.Abstractions;

namespace AccountService.Infrastructure.Consumers
{
    public class AntifraudConsumer(
        IAccountRepository accRepo,
        IInboxRepository inboxRepo,
        ILogger<AntifraudConsumer> logger)
        : IConsumerHandler
    {
        public async Task HandleAsync(byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            var baseEvent = JsonConvert.DeserializeObject<DomainEvent>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            if (baseEvent is null) return;

            var isProcessed = await inboxRepo.IsProcessedAsync(baseEvent.EventId, nameof(AuditConsumer));
            if (isProcessed)
            {
                logger.LogInformation("Event {EventId} already processed by {ConsumerName}, skipping", baseEvent.EventId, nameof(AuditConsumer));
                return;
            }

            await using var transaction = await accRepo.BeginTransactionAsync();
            try
            {
                switch (baseEvent)
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
                        logger.LogWarning("Unknown event type: {Type}", baseEvent.GetType().Name);
                        break;
                }
                await inboxRepo.MarkAsProcessedAsync(baseEvent.EventId, nameof(AntifraudConsumer), transaction);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Error processing event {EventId}", baseEvent.EventId);
                throw;
            }
        }
    }
}
