using AccountService.Application.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AccountService.Infrastructure.Services;

public class RabbitMqService(IConnection connection) : IRabbitMqService
{
    public void Publish(string exchange, string routingKey, byte[] body)
    {
        using var channel = connection.CreateModel();

        channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );
    }

    public void Subscribe(string queue, Action<byte[]> handler)
    {
        using var channel = connection.CreateModel();

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, ea) => handler(ea.Body.ToArray());

        channel.BasicConsume(
            queue: queue,
            autoAck: true,
            consumer: consumer
        );
    }
}