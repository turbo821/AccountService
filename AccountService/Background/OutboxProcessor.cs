using AccountService.Application.Abstractions;
using Hangfire;
using System.Text;

namespace AccountService.Background;

public class OutboxProcessor(IOutboxRepository repo, IRabbitMqService rabbitMqService, ILogger<OutboxProcessor> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessOutboxMessages()
    {
        logger.LogInformation("Starting OutboxProcessor...");

        var messages = await repo.GetMessagesAsync();
        logger.LogInformation("{Count} message(s) fetched from outbox", messages.Count);

        foreach (var msg in messages)
        {
            logger.LogInformation("Publishing message {MessageId} to exchange '{Exchange}' with routing key '{RoutingKey}'",
                msg.Id, msg.Exchange, msg.RoutingKey);
            try
            {
                var eventBytes = Encoding.UTF8.GetBytes(msg.Payload);
                await rabbitMqService.Publish(msg.Exchange, msg.RoutingKey, eventBytes);

                logger.LogInformation("Message {MessageId} published successfully", msg.Id);

                await repo.MarkProcessedAsync(msg.Id);
                logger.LogInformation("Message {MessageId} marked as processed", msg.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish message {MessageId}. It will be retried", msg.Id);
            }
        }

        logger.LogInformation("OutboxProcessor finished");
    }
}