using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using AccountService.Features.Accounts.Contracts;
using Dapper;
using Hangfire;
using System.Data;
using System.Text;
using System.Text.Json;

namespace AccountService.Infrastructure.Persistence.Background;

public class OutboxProcessor(IDbConnection dbConnection, IRabbitMqService rabbitMqService, ILogger logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ProcessOutboxMessages()
    {
        var messages = await dbConnection.QueryAsync<OutboxMessage>(
            @"SELECT * FROM outbox_messages WHERE processed_at is NULL ORDER BY occurred_at LIMIT 100");

        foreach (var msg in messages)
        {
            switch (msg.Type)
            {
                case nameof(AccountOpened):
                    //var accountOpenedEvent = JsonSerializer.Deserialize<AccountOpened>(msg.Payload);
                    //var accountOpenedBytes = JsonSerializer.SerializeToUtf8Bytes(accountOpenedEvent);
                    var eventBytes = Encoding.UTF8.GetBytes(msg.Payload);
                    rabbitMqService.Publish("account.events", "account.opened", eventBytes);
                    break;

                case nameof(MoneyCredited):
                    var moneyCreditedEvent = JsonSerializer.Deserialize<MoneyCredited>(msg.Payload);
                    var moneyCreditedBytes = JsonSerializer.SerializeToUtf8Bytes(moneyCreditedEvent);
                    rabbitMqService.Publish("account.events", "money.credited", moneyCreditedBytes);
                    break;

                // Другие типы событий...
            }

            await dbConnection.ExecuteAsync(
                "UPDATE outbox_messages SET processed_at = @Now WHERE id = @Id",
                new { Now = DateTime.UtcNow, msg.Id });
        }
    }
}