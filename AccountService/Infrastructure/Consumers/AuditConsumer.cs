using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;

namespace AccountService.Infrastructure.Consumers;

public class AuditConsumer(IInboxRepository repo, ILogger<AuditConsumer> logger) : IConsumerHandler
{
    public async Task HandleAsync(DomainEvent @event, string eventType)
    {
        var isProcessed = await repo.IsProcessedAsync(@event.EventId, nameof(AuditConsumer));
        if (isProcessed)
        {
            logger.LogInformation("Event {EventId} already processed by {ConsumerName}, skipping", @event.EventId, nameof(AuditConsumer));
            return;
        }

        await using var transaction = await repo.BeginTransactionAsync();
        try
        {
            await repo.AddAuditAsync(@event, eventType, transaction);
            await repo.MarkAsProcessedAsync(@event.EventId, nameof(AuditConsumer), transaction);

            await transaction.CommitAsync();
            logger.LogInformation("Audit event stored: {EventId}", @event.EventId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
    }
}