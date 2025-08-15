using AccountService.Application.Abstractions;
using Hangfire;
using System.Text;

namespace AccountService.Background;

public class OutboxProcessor(IOutboxRepository repo, IRabbitMqService rabbitMqService)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessOutboxMessages()
    {
        var messages = await repo.GetMessagesAsync();

        foreach (var msg in messages)
        {
            var eventBytes = Encoding.UTF8.GetBytes(msg.Payload);
            rabbitMqService.Publish(msg.Exchange, msg.RoutingKey, eventBytes);

            await repo.MarkProcessedAsync(msg.Id);
        }
    }
}