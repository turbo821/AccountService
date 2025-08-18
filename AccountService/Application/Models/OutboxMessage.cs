using JetBrains.Annotations;

namespace AccountService.Application.Models;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class OutboxMessage
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public required string Exchange { get; set; }
    public required string RoutingKey { get; set; }
    public bool IsDeadLetter { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}