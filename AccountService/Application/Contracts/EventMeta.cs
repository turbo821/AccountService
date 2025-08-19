namespace AccountService.Application.Contracts;

/// <summary>
/// Метаданные события.
/// </summary>
/// <param name="Source">Источник события (например, имя сервиса или модуля).</param>
/// <param name="CorrelationId">Идентификатор корреляции для связывания связанных событий.</param>
/// <param name="CausationId">Идентификатор исходного события, которое вызвало это событие.</param>
/// <param name="Version">Версия формата события, по умолчанию "v1".</param>
public record EventMeta(
    string Source,
    Guid CorrelationId,
    Guid? CausationId,
    string Version = "v1"
);