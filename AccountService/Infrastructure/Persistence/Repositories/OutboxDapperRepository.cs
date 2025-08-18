using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using AccountService.Application.Models;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace AccountService.Infrastructure.Persistence.Repositories;

public class OutboxDapperRepository(IDbConnection connection) : IOutboxRepository
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
            Payload = JsonConvert.SerializeObject(@event, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }),
            @event.OccurredAt
        }, transaction);
    }

    public async Task<List<OutboxMessage>> GetMessagesAsync(int limit)
    {
        const string sql = 
            """
                SELECT * FROM outbox_messages
                WHERE processed_at is NULL AND NOT is_dead_letter
                ORDER BY occurred_at LIMIT @Limit
            """
        ;

        return (await connection.QueryAsync<OutboxMessage>(sql, new { Limit = limit })).ToList();
    }

    public async Task MarkProcessedAsync(Guid id)
    {
        await connection.ExecuteAsync(
            "UPDATE outbox_messages SET processed_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = id });
    }

    public async Task<int> GetPendingCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM outbox_messages WHERE processed_at IS NULL AND NOT is_dead_letter";
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task MarkDeadLetterAsync(Guid messageId)
    {
        const string sql =
            """
            UPDATE outbox_messages
            SET is_dead_letter = TRUE
            WHERE id = @Id
            """;

        await connection.ExecuteAsync(sql, new { Id = messageId });
    }
}