using AccountService.Application.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AccountService.Infrastructure.Services;

public class RabbitMqService(IConnectionFactory connectionFactory) : IRabbitMqService
{
    private readonly IConnection _connection = connectionFactory.CreateConnectionAsync().Result;

    public async Task Publish(string exchange, string routingKey, byte[] body)
    {
        await using var channel = await _connection.CreateChannelAsync();

        await channel.BasicPublishAsync(
            exchange: exchange, 
            routingKey: routingKey, 
            body: body);
    }

    public async Task Subscribe(string queue, Action<byte[]> handler)
    {
        await using var channel = await _connection.CreateChannelAsync();

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += (_, ea) =>
        {
            handler(ea.Body.ToArray());
            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: true,
            consumer: consumer
        );
    }
}