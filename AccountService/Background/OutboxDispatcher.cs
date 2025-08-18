using AccountService.Application.Abstractions;
using System.Text;
namespace AccountService.Background;

public class OutboxDispatcher(IOutboxRepository repo,
    IRabbitMqHealthChecker healthChecker, 
    IBrokerService brokerService, 
    ILogger<OutboxDispatcher> logger)
{
    private const int MaxRetries = 5;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(2);

    public async Task ProcessOutboxMessages()
    {
        logger.LogInformation("Starting OutboxDispatcher...");

        if (!await healthChecker.IsAliveAsync())
        {
            logger.LogWarning("RabbitMQ is not available. OutboxDispatcher will skip this run.");
            return;
        }

        var messages = await repo.GetMessagesAsync();
        logger.LogInformation("{Count} message(s) fetched from outbox", messages.Count);

        foreach (var msg in messages)
        {
            logger.LogInformation("Publishing message {MessageId} to exchange '{Exchange}' with routing key '{RoutingKey}'",
                msg.Id, msg.Exchange, msg.RoutingKey);

            var eventBytes = Encoding.UTF8.GetBytes(msg.Payload);
            var retries = 0;

            while (retries < MaxRetries)
            {
                try
                {
                    await brokerService.Publish(msg.Exchange, msg.RoutingKey, msg.Type, eventBytes);
                    logger.LogInformation("Message {MessageId} published successfully", msg.Id);

                    await repo.MarkProcessedAsync(msg.Id);
                    logger.LogInformation("Message {MessageId} marked as processed", msg.Id);

                    retries = MaxRetries;
                }
                catch (Exception ex)
                {
                    retries++;
                    logger.LogError(ex, "Failed to publish message {MessageId}. Retry {Retry}/{MaxRetries}", msg.Id, retries, MaxRetries);

                    if (retries >= MaxRetries)
                    {
                        await repo.MarkDeadLetterAsync(msg.Id);
                        logger.LogError("Message {MessageId} moved to dead-letter queue", msg.Id);
                        break;
                    }

                    await Task.Delay(_delay);
                }
            }
        }

        logger.LogInformation("OutboxDispatcher finished");
    }
}