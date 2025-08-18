using AccountService.Application.Abstractions;
using AccountService.Infrastructure.Consumers;

namespace AccountService.Background;

/// <summary>
/// Сервис для распределения потребителей по очередям
/// </summary>
public class ConsumerHostedService(IServiceScopeFactory scopeFactory,
    IBrokerService broker) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await SubscribeConsumer("account.antifraud", typeof(AntifraudConsumer));
        await SubscribeConsumer("account.audit", typeof(AuditConsumer));
    }

    private async Task SubscribeConsumer(string queueName, Type consumerType)
    {
        await broker.Subscribe(queueName, async (body, type) =>
        {
            using var scope = scopeFactory.CreateScope();
            var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<IConsumerHandler>>();

            foreach (var handler in handlers)
            {
                if (handler.GetType() != consumerType) continue;
                await handler.HandleAsync(body, type);
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}