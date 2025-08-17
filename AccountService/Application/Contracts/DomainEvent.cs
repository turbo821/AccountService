namespace AccountService.Application.Contracts;

public record DomainEvent(Guid EventId, DateTime OccurredAt)
{
    public EventMeta? Meta { get; set; }
}