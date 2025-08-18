namespace AccountService.Application.Contracts;

/// <summary>
/// Базовый класс для всех доменных событий.
/// </summary>
public record DomainEvent(Guid EventId, DateTime OccurredAt)
{
    public EventMeta? Meta { get; set; }
}