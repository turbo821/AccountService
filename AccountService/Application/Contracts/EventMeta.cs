namespace AccountService.Application.Contracts;

public record EventMeta(
    string Source,
    Guid CorrelationId,
    Guid? CausationId,
    string Version = "v1"
);