using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace AccountService.Infrastructure.Consumers;

public class AuditConsumer(IInboxRepository repo, ILogger<AuditConsumer> logger) : IConsumerHandler
{
    public async Task HandleAsync(byte[] body)
    {
        var json = Encoding.UTF8.GetString(body);
        var baseEvent = JsonConvert.DeserializeObject<DomainEvent>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

        if (baseEvent == null) return;

        var isProcessed = await repo.IsProcessedAsync(baseEvent.EventId, nameof(AuditConsumer));
        if (isProcessed)
        {
            logger.LogInformation("Event {EventId} already processed by {ConsumerName}, skipping", baseEvent.EventId, nameof(AuditConsumer));
            return;
        }

        await using var transaction = await repo.BeginTransactionAsync();
        try
        {
            await repo.AddAuditAsync(baseEvent, transaction);
            await repo.MarkAsProcessedAsync(baseEvent.EventId, nameof(AuditConsumer), transaction);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        

        logger.LogInformation("Audit event stored: {EventId}", baseEvent.EventId);
    }
}