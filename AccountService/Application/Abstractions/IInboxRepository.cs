using AccountService.Application.Contracts;
using System.Data;
using System.Data.Common;

namespace AccountService.Application.Abstractions;

public interface IInboxRepository
{
    Task<bool> IsProcessedAsync(Guid messageId, string handler, IDbTransaction? transaction = null);
    Task MarkAsProcessedAsync(Guid messageId, string handler, IDbTransaction? transaction = null);
    Task AddAuditAsync(DomainEvent @event, IDbTransaction? transaction = null);
    Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
}