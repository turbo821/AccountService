using AccountService.Application.Models;
using AccountService.Controllers;
using AccountService.Features.Accounts;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Testcontainers.PostgreSql;

namespace AccountService.Tests.IntegrationTests;

public class ParallelTransferTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private AccountIntegrationTestsWebFactory _factory = null!;
    private HttpClient _client = null!;

    public Guid Account1Id { get; private set; }
    public Guid Account2Id { get; private set; }

    public async Task InitializeAsync()
    {
        await _pgContainer.StartAsync();

        _factory = new AccountIntegrationTestsWebFactory(_pgContainer);

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        var ownerId = Guid.NewGuid();
        var createAccountCmd1 = new
        {
            OwnerId = ownerId,
            Type = "Checking",
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
        resp1.EnsureSuccessStatusCode();

        var resp2 = await _client.PostAsJsonAsync("/accounts", createAccountCmd2);
        resp2.EnsureSuccessStatusCode();

        var account1 = (await resp1.Content.ReadFromJsonAsync<MbResult<AccountIdDto>>())!.Data!;
        var account2 = (await resp2.Content.ReadFromJsonAsync<MbResult<AccountIdDto>>())!.Data!;

        // Пополняем каждый аккаунт балансом 1000
        await AddTransaction(account1.AccountId, 1000m, "USD");
        await AddTransaction(account2.AccountId, 1000m, "USD");

        Account1Id = account1.AccountId;
        Account2Id = account2.AccountId;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await _pgContainer.DisposeAsync();
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
        const int parallelCount = 50;
        decimal transferAmount = 10m;
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < parallelCount; i++)
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

            var cmdBack = new
            {
                FromAccountId = Account2Id,
                ToAccountId = Account1Id,
                Amount = transferAmount,
                Currency = "USD",
                Description = "Test transfer back"
            };

            tasks.Add(_client.PostAsJsonAsync("/accounts/transfer", cmdBack));
        }

        await Task.WhenAll(tasks);

        // Проверяем, что все запросы успешны
        foreach (var task in tasks)
        {
            var response = task.Result;
            Assert.True(response.IsSuccessStatusCode, $"Expected success status code but got {response.StatusCode}");
        }

        // Получаем балансы после всех переводов
        var account1Resp = await _client.GetAsync($"/accounts/{Account1Id}");
        var account2Resp = await _client.GetAsync($"/accounts/{Account2Id}");

        account1Resp.EnsureSuccessStatusCode();
        account2Resp.EnsureSuccessStatusCode();

        var account1 = (await account1Resp.Content.ReadFromJsonAsync<MbResult<AccountDto>>())!.Data!;
        var account2 = (await account2Resp.Content.ReadFromJsonAsync<MbResult<AccountDto>>())!.Data!;

        var totalBalance = account1.Balance + account2.Balance;

        Assert.Equal(2000m, totalBalance);
    }
}

