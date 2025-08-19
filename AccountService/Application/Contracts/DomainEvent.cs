namespace AccountService.Application.Contracts;

/// <summary>
/// Базовый класс для всех доменных событий.
/// </summary>
/// <param name="EventId">Уникальный идентификатор события.</param>
/// <param name="OccurredAt">Дата и время возникновения события.</param>
public record DomainEvent(Guid EventId, DateTime OccurredAt)
{
    /// <summary>
    /// Метаданные события, такие как источник, корреляция и причинное событие.
    /// </summary>
    public EventMeta? Meta { get; set; }
}