using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
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
            logger.LogWarning("RabbitMQ is not available. Skipping processing.");
            return;
        }

        var messages = await repo.GetMessagesAsync(100);
        logger.LogInformation("{Count} message(s) fetched from outbox", messages.Count);

        foreach (var msg in messages)
        {
            var eventMeta = ExtractMeta(msg.Payload);
            var sw = Stopwatch.StartNew();
            var retries = 0;

            logger.LogInformation("Publishing message {@Message} with correlationId {CorrelationId}",
                new { msg.Id, msg.Type, msg.Exchange, msg.RoutingKey },
                eventMeta?.CorrelationId);

            while (retries < MaxRetries)
            {
                try
                {
                    var eventBytes = Encoding.UTF8.GetBytes(msg.Payload);
                    
                    await brokerService.Publish(msg.Exchange, msg.RoutingKey, msg.Type, eventBytes);
                    sw.Stop();
                    logger.LogInformation("Message published {@Message} successfully in {Latency}ms",
                        new { msg.Id, msg.Type, msg.Exchange, msg.RoutingKey }, sw.ElapsedMilliseconds);

                    await repo.MarkProcessedAsync(msg.Id);

                    logger.LogInformation("Message {@Message} marked as processed", new { msg.Id, msg.Type });
                    retries = MaxRetries;
                }
                catch (Exception ex)
                {
                    retries++;
                    sw.Stop();
                    logger.LogError(ex,
                        "Failed to publish message {@Message} retry {Retry}/{MaxRetries} latency {Latency}ms",
                        new { msg.Id, msg.Type, msg.Exchange, msg.RoutingKey, eventMeta?.CorrelationId },
                        retries,
                        MaxRetries,
                        sw.ElapsedMilliseconds);

                    if (retries >= MaxRetries)
                    {
                        await repo.MarkDeadLetterAsync(msg.Id);
                        logger.LogError("Message {@Message} moved to dead-letter queue after {Retries} retries",
                            new { msg.Id, msg.Type, eventMeta?.CorrelationId }, retries);
                        break;
                    }

                    await Task.Delay(_delay);
                    sw.Restart();
                }
            }
        }

        logger.LogInformation("OutboxDispatcher finished");
    }

    private static EventMeta? ExtractMeta(string payload)
    {
        try
        {
            var domainEvent = JsonConvert.DeserializeObject<DomainEvent>(payload, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            return domainEvent?.Meta;
        }
        catch
        {
            // ignored
        }

        return null;
    }
}