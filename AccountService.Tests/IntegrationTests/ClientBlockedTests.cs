using AccountService.Application.Abstractions;
using AccountService.Application.Contracts;
using AccountService.Application.Models;
using AccountService.Background;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Contracts;
using AccountService.Tests.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace AccountService.Tests.IntegrationTests;

[Collection("IntegrationTests")]
[Trait("Category", "Integration")]
public class ClientBlockedTests(IntegrationTestsWebFactory factory, ITestOutputHelper output)
    : IClassFixture<IntegrationTestsWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private Guid OwnerId { get; } = Guid.NewGuid();

    [Fact]
    public async Task ClientBlockedPreventsDebit_ThenUnblockedAllowsDebit()
    {
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var outboxDispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
        var accountId = await InitializeAccountAsync();

        // Block the client
        var clientBlocked = new ClientBlocked(Guid.NewGuid(), DateTime.UtcNow, OwnerId);
        clientBlocked.Meta = new EventMeta
        (
            "test",
            Guid.NewGuid(),
            clientBlocked.EventId
        );
        await repo.AddAsync(clientBlocked, "account.events", "client.blocked");
        await outboxDispatcher.ProcessOutboxMessages();

        await Task.Delay(2); // Wait for the consumer to process in the background the event for sure
        output.WriteLine("Client blocked");

        var debitAfterBlocked = new
        {
            Amount = 110,
            Currency = "USD",
            Type = TransactionType.Debit,
            Description = "debit transaction"
        };
        var response = await _client.PostAsJsonAsync($"/accounts/{accountId}/transactions", debitAfterBlocked);
        output.WriteLine($"API response when funds are debited: {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var outboxMessages = await repo.GetMessagesAsync(100);
        var moneyDebitedExists = outboxMessages.Any(m => m.Type == nameof(MoneyDebited));
        output.WriteLine($"In the MoneyDebited message outbox: {moneyDebitedExists}");
        Assert.False(moneyDebitedExists);

        // Unblock the client
        var clientUnblocked = new ClientUnblocked(Guid.NewGuid(), DateTime.UtcNow, OwnerId)
        {
            Meta = new EventMeta("test", Guid.NewGuid(), Guid.NewGuid())
        };
        await repo.AddAsync(clientUnblocked, "account.events", "client.unblocked");
        await outboxDispatcher.ProcessOutboxMessages();

        await Task.Delay(2); // Wait for the consumer to process in the background the event for sure
        output.WriteLine("\nClient unblocked");

        var debitAfterUnblocked = new
        {
            Amount = 130,
            Currency = "USD",
            Type = TransactionType.Debit,
            Description = "debit transaction"
        };
        var response2 = await _client.PostAsJsonAsync($"/accounts/{accountId}/transactions", debitAfterUnblocked);
        output.WriteLine($"API response when funds are debited: {(int)response2.StatusCode} {response2.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var accountResponse = await _client.GetAsync($"/accounts/{accountId}");
        accountResponse.EnsureSuccessStatusCode();

        var account = (await accountResponse.Content.ReadFromJsonAsync<MbResult<AccountDto>>())!.Data!;
        output.WriteLine($"Account Balance: {(int)account.Balance}");
        Assert.Equal(account.Balance, 1000m - debitAfterUnblocked.Amount);
    }

    private async Task<Guid> InitializeAccountAsync()
    {
        var createAccountCmd = new
        {
            OwnerId = OwnerId.ToString(),
            Type = AccountType.Checking,
            Currency = "USD",
            InterestRate = (decimal?)null
        };

        var response = await _client.PostAsJsonAsync("/accounts", createAccountCmd);
        response.EnsureSuccessStatusCode();

        var account = (await response.Content.ReadFromJsonAsync<MbResult<AccountIdDto>>())!.Data!;
        await AddTransaction(account.AccountId, 1000m, "USD", TransactionType.Credit);

        return account.AccountId;
    }

    private async Task AddTransaction(Guid accountId, decimal amount, string currency, TransactionType type)
    {
        var transactionCmd = new
        {
            Amount = amount,
            Currency = currency,
            Type = type,
            Description = "Initial credit"
        };

        var resp = await _client.PostAsJsonAsync($"/accounts/{accountId}/transactions", transactionCmd);
        resp.EnsureSuccessStatusCode();
    }
}