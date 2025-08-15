namespace AccountService.Application.Models;

public abstract record DomainEvent(Guid EventId, DateTime OccurredAt);