using AccountService.Application.Abstractions;
using AccountService.Application.Models;
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
public class TransferTests(IntegrationTestsWebFactory factory, ITestOutputHelper output)
    : IClassFixture<IntegrationTestsWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private Guid Account1Id { get; set; }
    private Guid Account2Id { get; set; }

    [Fact]
    public async Task TransferEmitsSingleEvent()
    {
        const decimal transferAmount = 10m;
        var scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        Account1Id = await InitializeAccountAsync();
        Account2Id = await InitializeAccountAsync();

        for (var i = 0; i < 50; i++)
        {
            var cmd = new
            {
                FromAccountId = Account1Id,
                ToAccountId = Account2Id,
                Amount = transferAmount,
                Currency = "USD",
                Description = $"Test transfer {i + 1}"
            };

            var response = await _client.PostAsJsonAsync("/accounts/transfer", cmd);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        output.WriteLine("All Transfers with 200 OK\n");

        var messages = await repo.GetMessagesAsync(300);

        var transferEvents = messages.Where(m => m.Type == nameof(TransferCompleted)).ToList();

        output.WriteLine($"Total outbox messages: {messages.Count}");
        output.WriteLine($"MoneyTransferred events: {transferEvents.Count}");

        Assert.Equal(50, transferEvents.Count);

        var account1Resp = await _client.GetAsync($"/accounts/{Account1Id}");
        var account2Resp = await _client.GetAsync($"/accounts/{Account2Id}");

        account1Resp.EnsureSuccessStatusCode();
        account2Resp.EnsureSuccessStatusCode();

        var account1 = (await account1Resp.Content.ReadFromJsonAsync<MbResult<AccountDto>>())!.Data!;
        var account2 = (await account2Resp.Content.ReadFromJsonAsync<MbResult<AccountDto>>())!.Data!;

        output.WriteLine($"\nAccount 1 Balance: {(int)account1.Balance}");
        output.WriteLine($"Account 2 Balance: {(int)account2.Balance}");

        var totalBalance = account1.Balance + account2.Balance;
        Assert.Equal(2000m, totalBalance);
    }

    private async Task<Guid> InitializeAccountAsync()
    {
        var createAccountCmd = new
        {
            OwnerId = Guid.NewGuid(),
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