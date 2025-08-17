using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace AccountService.Infrastructure.Services;

public class RabbitMqService(IConnectionFactory connectionFactory) : IBrokerService
{
    private readonly IConnection _connection = connectionFactory.CreateConnectionAsync().Result;

    public async Task Publish(string exchange, string routingKey, byte[] body)
    {
        await using var channel = await _connection.CreateChannelAsync();

        var props = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object>()!
        };

        var payload = Encoding.UTF8.GetString(body);
        var domainEvent = JsonConvert.DeserializeObject<DomainEvent>(payload);

        if (domainEvent?.Meta?.CorrelationId != null)
            props.Headers["X-Correlation-Id"] = domainEvent?.Meta?.CorrelationId.ToString();
        if (domainEvent?.Meta?.CausationId != null)
            props.Headers["X-Causation-Id"] = domainEvent?.Meta?.CausationId.ToString();

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