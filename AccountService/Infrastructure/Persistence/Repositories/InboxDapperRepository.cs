using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;
using System.Data.Common;

namespace AccountService.Infrastructure.Persistence.Repositories;

public class InboxDapperRepository(IDbConnection connection) : IInboxRepository
{
    public async Task<bool> IsProcessedAsync(Guid messageId, string handler, IDbTransaction? transaction = null)
    {
        const string sql = 
            """
               SELECT COUNT(*) 
               FROM inbox_consumed 
               WHERE message_id = @MessageId AND handler = @Handler
            """;

        var count = await connection.ExecuteScalarAsync<int>(
            sql,
            new { MessageId = messageId, Handler = handler },
            transaction
        );

        return count > 0;
    }

    public async Task MarkAsProcessedAsync(Guid messageId, string handler, IDbTransaction? transaction = null)
    {
        const string sql = 
            """
               INSERT INTO inbox_consumed (message_id, processed_at, handler)
               VALUES (@MessageId, @ProcessedAt, @Handler)
            """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                MessageId = messageId,
                ProcessedAt = DateTime.UtcNow,
                Handler = handler
            },
            transaction
        );
    }

    public async Task AddAuditAsync(DomainEvent @event, string eventType, IDbTransaction? transaction = null)
    {
        const string sql = 
            """
               INSERT INTO audit_events (id, type, payload, occurred_at) 
               VALUES (@Id, @Type, @Payload::jsonb, @OccurredAt);
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = @event.EventId,
            Type = eventType,
            Payload = JsonConvert.SerializeObject(@event, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }),
            @event.OccurredAt
        }, transaction);
    }

    public async Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
    {
        if (connection is not DbConnection conn)
            throw new InvalidOperationException("Connection must be DbConnection");

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync();

        return await conn.BeginTransactionAsync(isolationLevel);
    }

    public async Task AddDearLetterAsync(Guid messageId, string type, string payload, string error,
        IDbTransaction? transaction = null)
    {
        const string sql = 
            """
               INSERT INTO inbox_dead_letters (message_id, received_at, type, payload, error)
               VALUES (@MessageId, @ReceivedAt, @Type, @Payload::jsonb, @Error);
            """;

        await connection.ExecuteAsync(sql, new
        {
            MessageId = messageId,
            ReceivedAt = DateTime.UtcNow,
            Type = type,
            Payload = payload,
            Error = error
        }, transaction);
    }
}