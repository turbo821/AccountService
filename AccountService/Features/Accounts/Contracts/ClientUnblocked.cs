namespace AccountService.Features.Accounts.Contracts;

public record ClientUnblocked(
    Guid EventId,
    DateTime OccurredAt,
    Guid ClientId
);