using AccountService.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using AccountService.Background;
using AccountService.Features.Accounts.Contracts;

namespace AccountService.Tests.IntegrationTests;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class OutboxTests(IntegrationTestsWebFactory factory) : IClassFixture<IntegrationTestsWebFactory>
{
    [Fact]
    public async Task OutboxPublishesAfterFailure()
    {
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var outboxDispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();

        await factory.StopRabbitMqAsync();

        var @event = new MoneyCredited(Guid.NewGuid(), DateTime.UtcNow,
            Guid.NewGuid(), 100,
            "RUB", Guid.NewGuid(), "ok");
        @event.Meta = new EventMeta("tests", Guid.NewGuid(), @event.EventId);

        await repo.AddAsync(@event, "account.events", "money.credited");

        await outboxDispatcher.ProcessOutboxMessages();

        var messagesBeforeStart = await repo.GetMessagesAsync();
        Assert.NotEmpty(messagesBeforeStart);
        
        await factory.StartRabbitMqAsync();

        await outboxDispatcher.ProcessOutboxMessages();

        var messagesAfterStart = await repo.GetMessagesAsync();
        Assert.Empty(messagesAfterStart);
    }
}