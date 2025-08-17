namespace AccountService.Application.Abstractions;

public interface IBrokerService
{
    Task Publish(string exchange, string routingKey, byte[] body);
    Task Subscribe(string queue, Action<byte[]> handler);
}