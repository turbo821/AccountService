using AccountService.Application.Abstractions;
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
using RabbitMQ.Client;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using IConnectionFactory = RabbitMQ.Client.IConnectionFactory;

namespace AccountService.Tests.IntegrationTests;

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
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        await WaitForPostgresReady(_postgresContainer.GetConnectionString());

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqContainer.Hostname,
            Port = _rabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest"
        };
        var port = _rabbitMqContainer.GetMappedPublicPort(5672);
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Exchange
        await channel.ExchangeDeclareAsync("account.events", ExchangeType.Topic, durable: true);

        // Очереди
        var queues = new[]
        {
            "account.crm",
            "account.notifications",
            "account.antifraud",
            "account.audit"
        };

        foreach (var queue in queues)
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);

        // Bindings
        await channel.QueueBindAsync("account.crm", "account.events", "account.*");
        await channel.QueueBindAsync("account.notifications", "account.events", "money.*");
        await channel.QueueBindAsync("account.antifraud", "account.events", "client.*");
        await channel.QueueBindAsync("account.audit", "account.events", "#");
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
            DeletePostgresqlServices(services);
            DeleteHangfireServices(services);
            DeleteRabbitMqServices(services);

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
            services.AddHangfireWithPostgres(newConfig);
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

    private static void DeletePostgresqlServices(IServiceCollection services)
    {
        var descriptorDbConnection = services.SingleOrDefault(
            d => d.ServiceType == typeof(IDbConnection));
        if (descriptorDbConnection != null)
            services.Remove(descriptorDbConnection);

        var fmServices = services
            .Where(d =>
                d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("FluentMigrator")).ToList();
        if (fmServices.Count == 0) return;
        foreach (var runner in fmServices)
            services.Remove(runner);
    }
    private static void DeleteHangfireServices(IServiceCollection services)
    {
        var hangfireServices = services
            .Where(d => d.ServiceType.FullName != null &&
                        d.ServiceType.FullName.Contains("Hangfire")).ToList();

        foreach (var descriptor in hangfireServices)
            services.Remove(descriptor);
    }

    private static void DeleteRabbitMqServices(IServiceCollection services)
    {

        var descriptorRabbitMq = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionFactory));
        if (descriptorRabbitMq != null)
            services.Remove(descriptorRabbitMq);

        var brokerServiceDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(IBrokerService));
        if (brokerServiceDescriptor != null)
            services.Remove(brokerServiceDescriptor);

        var consumerHandlers = services
            .Where(d => d.ServiceType == typeof(IConsumerHandler))
            .ToList();
        foreach (var descriptor in consumerHandlers)
            services.Remove(descriptor);

        var healthCheckDescriptor = services
            .SingleOrDefault(d => d.ServiceType == typeof(IRabbitMqHealthChecker));
        if (healthCheckDescriptor != null)
            services.Remove(healthCheckDescriptor);

        var hostedServiceDescriptor = services
            .SingleOrDefault(d => d.ImplementationType == typeof(ConsumerHostedService));
        if (hostedServiceDescriptor != null)
            services.Remove(hostedServiceDescriptor);
    }

    public async Task StopRabbitMqAsync()
    {
        await _rabbitMqContainer.StopAsync();
    }

    public async Task StartRabbitMqAsync()
    {
        await _rabbitMqContainer.StartAsync();
        var port = _rabbitMqContainer.GetMappedPublicPort(5672);
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