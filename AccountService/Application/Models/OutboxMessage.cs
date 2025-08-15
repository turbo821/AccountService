namespace AccountService.Application.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}