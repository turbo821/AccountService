using AccountService.Application.Contracts;
using System.Data;
using System.Data.Common;

namespace AccountService.Application.Abstractions;

public interface IInboxRepository
{
    Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified);

    Task<bool> IsProcessedAsync(Guid messageId, string handler, IDbTransaction? transaction = null);
    Task MarkAsProcessedAsync(Guid messageId, string handler, IDbTransaction? transaction = null);
    Task AddAuditAsync(DomainEvent @event, string eventType, IDbTransaction? transaction = null);

    Task AddDearLetterAsync(Guid messageId, string type, string payload, string error,
        IDbTransaction? transaction = null);
}