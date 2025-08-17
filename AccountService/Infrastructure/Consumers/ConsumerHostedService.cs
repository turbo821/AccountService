using AccountService.Application.Abstractions;

namespace AccountService.Infrastructure.Consumers;

public interface IConsumerHostedService : IHostedService { }

public class ConsumerHostedService(IEnumerable<IConsumerHandler> handlers,
    IBrokerService broker) : IConsumerHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await broker.Subscribe("account.antifraud", async body =>
        {
            foreach (var handler in handlers)
            {
                if (handler.GetType() != typeof(AntifraudConsumer)) continue;
                await handler.HandleAsync(body);
            }
        });

        await broker.Subscribe("account.audit", async body =>
        {
            foreach (var handler in handlers)
            {
                if (handler.GetType() != typeof(AuditConsumer)) continue;
                await handler.HandleAsync(body);
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}