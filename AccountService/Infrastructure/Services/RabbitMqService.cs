using System.Diagnostics;
using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace AccountService.Infrastructure.Services;

/// <summary>
/// Сервис для публикации и подписки сообщений RabbitMQ
/// </summary>
public class RabbitMqService(IConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory, ILogger<RabbitMqService> logger)
    : IBrokerService, IAsyncDisposable
{
    private readonly List<IChannel> _channels = [];
    private readonly IConnection _connection = connectionFactory.CreateConnectionAsync().Result;

    /// <summary>
    /// Публикация сообщения
    /// </summary>
    public async Task Publish(string exchange, string routingKey, string eventType, byte[] body)
    {
        await using var channel = await _connection.CreateChannelAsync();

        var props = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object>()!,
            Type = eventType
        };

        var payload = Encoding.UTF8.GetString(body);
        var domainEvent = JsonConvert.DeserializeObject<DomainEvent>(payload);

        if (domainEvent?.Meta?.CorrelationId != null)
            props.Headers["X-Correlation-Id"] = domainEvent.Meta?.CorrelationId.ToString();
        if (domainEvent?.Meta?.CausationId != null)
            props.Headers["X-Causation-Id"] = domainEvent.Meta?.CausationId.ToString();

        await channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, 
            mandatory: false, basicProperties: props, body: body);
    }

    /// <summary>
    /// Подписка на RabbitMQ для потребителя
    /// </summary>
    public async Task Subscribe(string queue, Func<string, string, Task> handler)
    {
        var channel = await _connection.CreateChannelAsync();
        _channels.Add(channel);

        await channel.BasicQosAsync(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var sw = Stopwatch.StartNew();
            using var scope = scopeFactory.CreateScope();
            var inboxRepo = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            DomainEvent? domainEvent = null;
            var type = ea.BasicProperties.Type ?? "Unknown";

            try
            {
                domainEvent = JsonConvert.DeserializeObject<DomainEvent>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });

                if (domainEvent is null)
                    throw new Exception("Event is not serialized to DomainEvent");

                logger.LogInformation(
                    "Received event {@Event} correlationId {CorrelationId} eventId {EventId}",
                    type,
                    domainEvent.Meta?.CorrelationId,
                    domainEvent.EventId
                );

                if (domainEvent.Meta?.Version != "v1")
                {
                    await inboxRepo.AddDearLetterAsync(domainEvent.EventId,  type, json, $"Unsupported version: {domainEvent.Meta?.Version}");
                    logger.LogWarning("Unsupported version for event {EventId}, quarantined", domainEvent.EventId);

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                await handler(json, type);

                sw.Stop();
                logger.LogInformation(
                    "Processed event {@Event} correlationId {CorrelationId} eventId {EventId} in {Latency}ms",
                    type,
                    domainEvent.Meta?.CorrelationId,
                    domainEvent.EventId,
                    sw.ElapsedMilliseconds
                );

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                sw.Stop();
                if (domainEvent != null)
                {
                    await inboxRepo.AddDearLetterAsync(domainEvent.EventId, type, json, ex.Message);
                }

                logger.LogError(
                    ex,
                    "Error processing event {@Event} correlationId {CorrelationId} eventId {EventId} latency {Latency}ms",
                    type,
                    domainEvent?.Meta?.CorrelationId,
                    domainEvent?.EventId,
                    sw.ElapsedMilliseconds
                );

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer
        );
    }

    /// <summary>
    /// Закрытие каналов и соединения к RabbitMQ
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var channel in _channels)
        {
            try
            {
                await channel.CloseAsync();
                channel.Dispose();
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error channel dispose {Channel}", channel);
            }
        }
        _channels.Clear();
        try
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error connection dispose {Connection}", _connection);
        }
    }
}