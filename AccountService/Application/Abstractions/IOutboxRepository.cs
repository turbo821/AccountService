using System.Data;
using AccountService.Application.Contracts;
using AccountService.Application.Models;

namespace AccountService.Application.Abstractions;

public interface IOutboxRepository
{
    Task AddAsync(DomainEvent @event, string exchange, string routingKey, IDbTransaction? transaction = null);
    Task<List<OutboxMessage>> GetMessagesAsync(int limit);
    Task MarkProcessedAsync(Guid id);
    Task<int> GetPendingCountAsync();
    Task MarkDeadLetterAsync(Guid messageId);

}