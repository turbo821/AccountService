namespace AccountService.Application.Abstractions;

public interface IRabbitMqService
{
    void Publish(string exchange, string routingKey, byte[] body);
    void Subscribe(string queue, Action<byte[]> handler);
}