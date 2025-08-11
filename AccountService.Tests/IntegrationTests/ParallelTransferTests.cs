using AccountService.Application.Models;
using AccountService.Features.Accounts;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace AccountService.Tests.IntegrationTests;

public class ParallelTransferTests : IClassFixture<IntegrationTestsWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _testOutputHelper;

    public Guid Account1Id { get; private set; }
    public Guid Account2Id { get; private set; }

    public ParallelTransferTests(IntegrationTestsWebFactory factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
    }

    private async Task InitializeAccountsAsync()
    {
        var ownerId = Guid.NewGuid().ToString();
        var createAccountCmd1 = new
        {
            OwnerId = ownerId,
            Type = AccountType.Checking,
            Currency = "USD",
            InterestRate = (decimal?)null
        };
        var createAccountCmd2 = new
        {
            OwnerId = ownerId,
            Type = "Checking",
            Currency = "USD",
            InterestRate = (decimal?)null
        };

        var resp1 = await _client.PostAsJsonAsync("/accounts", createAccountCmd1);
        if (!resp1.IsSuccessStatusCode)
        {
            var errorBody = await resp1.Content.ReadAsStringAsync();
            Console.WriteLine($"Response error: {errorBody}");
        }
        resp1.EnsureSuccessStatusCode();

        var resp2 = await _client.PostAsJsonAsync("/accounts", createAccountCmd2);
        resp2.EnsureSuccessStatusCode();

        var account1 = (await resp1.Content.ReadFromJsonAsync<MbResult<AccountIdDto>>())!.Data!;
        var account2 = (await resp2.Content.ReadFromJsonAsync<MbResult<AccountIdDto>>())!.Data!;

        await AddTransaction(account1.AccountId, 1000m, "USD");
        await AddTransaction(account2.AccountId, 1000m, "USD");

        Account1Id = account1.AccountId;
        Account2Id = account2.AccountId;
    }

    private async Task AddTransaction(Guid accountId, decimal amount, string currency)
    {
        var transactionCmd = new
        {
            Amount = amount,
            Currency = currency,
            Type = "Debit",
            Description = "Initial deposit"
        };

        var resp = await _client.PostAsJsonAsync($"/accounts/{accountId}/transactions", transactionCmd);
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Transfer_ShouldMaintainTotalBalance_After50ParallelTransfers()
    {
        await InitializeAccountsAsync();
        
        const int parallelCount = 50;
        const decimal transferAmount = 10m;

        var tasks = new List<Task<HttpResponseMessage>>();

        for (var i = 0; i < parallelCount; i++)
        {
            var cmd = new
            {
                FromAccountId = Account1Id,
                ToAccountId = Account2Id,
                Amount = transferAmount,
                Currency = "USD",
                Description = "Test transfer"
            };

            tasks.Add(_client.PostAsJsonAsync("/accounts/transfer", cmd));
        }

        await Task.WhenAll(tasks);

        var statusCounts = tasks
            .Select(t => t.Result.StatusCode)
            .GroupBy(code => code)
            .ToDictionary(g => g.Key, g => g.Count());

        _testOutputHelper.WriteLine("Status codes summary:");
        foreach (var kvp in statusCounts)
        {
            _testOutputHelper.WriteLine($"{(int)kvp.Key} ({kvp.Key}): {kvp.Value}");
        }

        foreach (var status in statusCounts.Keys)
        {
            Assert.True(status is HttpStatusCode.OK or HttpStatusCode.Conflict,
                $"Unexpected status code {status}");
        }

        var account1Resp = await _client.GetAsync($"/accounts/{Account1Id}");
        var account2Resp = await _client.GetAsync($"/accounts/{Account2Id}");

        account1Resp.EnsureSuccessStatusCode();
        account2Resp.EnsureSuccessStatusCode();

        var account1 = (await account1Resp.Content.ReadFromJsonAsync<MbResult<AccountDto>>())!.Data!;
        var account2 = (await account2Resp.Content.ReadFromJsonAsync<MbResult<AccountDto>>())!.Data!;

        _testOutputHelper.WriteLine($"Account 1 Balance: {(int)account1.Balance}");
        _testOutputHelper.WriteLine($"Account 2 Balance: {(int)account2.Balance}");

        var totalBalance = account1.Balance + account2.Balance;
        Assert.Equal(2000m, totalBalance);
    }

    public async Task InitializeAsync()
    {
        await InitializeAccountsAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}