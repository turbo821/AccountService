using AccountService.Application.Contracts;

namespace AccountService.Application.Abstractions;

public interface IConsumerHandler
{
    Task HandleAsync(DomainEvent @event, string eventType);
}