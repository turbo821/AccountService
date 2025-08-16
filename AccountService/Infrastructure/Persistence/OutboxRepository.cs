using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using Dapper;
using Newtonsoft.Json;
using System.Data;

namespace AccountService.Infrastructure.Persistence;

public class OutboxRepository(IDbConnection connection) : IOutboxRepository
{
    public async Task AddAsync(DomainEvent @event, string exchange, string routingKey, IDbTransaction? transaction = null)
    {
        const string insertOutboxSql =
            """
               INSERT INTO outbox_messages (id, type, exchange, routing_key, payload, occurred_at)
               VALUES (@Id, @Type, @Exchange, @RoutingKey, @Payload::jsonb, @OccurredAt)
            """;

        await connection.ExecuteAsync(insertOutboxSql, new
        {
            Id = @event.EventId,
            Type = @event.GetType().Name,
            Exchange = exchange,
            RoutingKey = routingKey,
            Payload = JsonConvert.SerializeObject(@event),
            @event.OccurredAt
        }, transaction);
    }

    public async Task<List<OutboxMessage>> GetMessagesAsync(int limit = 100)
    {
        const string sql = "SELECT * FROM outbox_messages WHERE processed_at is NULL ORDER BY occurred_at LIMIT @Limit";

        return (await connection.QueryAsync<OutboxMessage>(sql, new { Limit = limit })).ToList();
    }

    public async Task MarkProcessedAsync(Guid id)
    {
        await connection.ExecuteAsync(
            "UPDATE outbox_messages SET processed_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = id });
    }
}