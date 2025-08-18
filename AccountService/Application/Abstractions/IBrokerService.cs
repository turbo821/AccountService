
using AccountService.Application.Contracts;

namespace AccountService.Application.Abstractions;

public interface IBrokerService
{
    Task Publish(string exchange, string routingKey, string eventType, byte[] body);
    Task Subscribe(string queue, Func<DomainEvent, string, Task> handler);
}