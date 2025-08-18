using AccountService.Background;
using AccountService.Extensions;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace AccountService.Tests.IntegrationTests.Common;

public class IntegrationTestsWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase(Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "test_db")
        .WithUsername(Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres")
        .WithPassword(Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres")
        .WithImage("postgres:17")
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilCommandIsCompleted("pg_isready -U postgres"))
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.1.3-management-alpine")
        .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
        .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
        .WithPortBinding(5672)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
        .Build();

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "IntegrationTests");

        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        await WaitForPostgresReady(_postgresContainer.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await _rabbitMqContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var testConnectionString = _postgresContainer.GetConnectionString();
        var rabbitHostName = _rabbitMqContainer.Hostname;
        var rabbitPort = _rabbitMqContainer.GetMappedPublicPort(5672);

        builder.ConfigureServices(services =>
        {
            var newConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = testConnectionString,
                    ["RabbitMQ:Host"] = rabbitHostName,
                    ["RabbitMQ:Port"] = rabbitPort.ToString(),
                    ["RabbitMQ:Username"] = "guest",
                    ["RabbitMQ:Password"] = "guest"
                })
                .Build();

            services.AddDatabase(newConfig);
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
            services.AddAuthorization();
            services.AddRabbitMq(newConfig);
            services.AddScoped<OutboxDispatcher>();
        });
    }

    private static async Task WaitForPostgresReady(string connectionString)
    {
        for (var i = 0; i < 50; i++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                return;
            }
            catch
            {
                await Task.Delay(500);
            }
        }
        throw new Exception("Postgres did not become ready in time.");
    }

    public async Task StopRabbitMqAsync()
    {
        await _rabbitMqContainer.StopAsync();
    }

    public async Task StartRabbitMqAsync()
    {
        await _rabbitMqContainer.StartAsync();
    }
}

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "test_user") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}