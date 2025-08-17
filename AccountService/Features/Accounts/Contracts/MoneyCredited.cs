using AccountService.Application.Contracts;

namespace AccountService.Features.Accounts.Contracts;

public record MoneyCredited(
    Guid EventId,
    DateTime OccurredAt,
    Guid AccountId,
    decimal Amount,
    string Currency,
    Guid OperationId,
    string Reason
) : DomainEvent(EventId, OccurredAt);