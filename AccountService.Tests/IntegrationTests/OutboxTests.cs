using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using AccountService.Background;
using AccountService.Features.Accounts;
using AccountService.Tests.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace AccountService.Tests.IntegrationTests;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class OutboxTests(IntegrationTestsWebFactory factory, ITestOutputHelper output) : IClassFixture<IntegrationTestsWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OutboxPublishesAfterFailure()
    {
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var outboxDispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();

        await factory.StopRabbitMqAsync();

        await InitializeAccountAsync();  // Initialize an account and make transaction (create AccountOpened and TransactionCreated events)

        await outboxDispatcher.ProcessOutboxMessages(); // This should not publish any messages since RabbitMQ is stopped

        var messagesBeforeStart = await repo.GetMessagesAsync(100);
        output.WriteLine($"Messages in outbox after RabbitMQ stop: {messagesBeforeStart.Count}");
        Assert.NotEmpty(messagesBeforeStart);

        await factory.StartRabbitMqAsync();

        await outboxDispatcher.ProcessOutboxMessages(); // This should now publish the messages that were in the outbox

        var messagesAfterStart = await repo.GetMessagesAsync(100);
        output.WriteLine($"Messages in outbox after RabbitMQ startup: {messagesAfterStart.Count}");
        Assert.Empty(messagesAfterStart);
    }

    private async Task InitializeAccountAsync()
    {
        var createAccountCmd = new
        {
            OwnerId = Guid.NewGuid().ToString(),
            Type = AccountType.Checking,
            Currency = "USD",
            InterestRate = (decimal?)null
        };

        var resp1 = await _client.PostAsJsonAsync("/accounts", createAccountCmd);

        resp1.EnsureSuccessStatusCode();
        var account1 = (await resp1.Content.ReadFromJsonAsync<MbResult<AccountIdDto>>())!.Data!;

        await AddTransaction(account1.AccountId, 1000m, "USD");
    }

    private async Task AddTransaction(Guid accountId, decimal amount, string currency)
    {
        var transactionCmd = new
        {
            Amount = amount,
            Currency = currency,
            Type = TransactionType.Credit,
            Description = "Initial credit"
        };

        var resp = await _client.PostAsJsonAsync($"/accounts/{accountId}/transactions", transactionCmd);
        resp.EnsureSuccessStatusCode();
    }
}