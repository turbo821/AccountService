using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AccountService.Extensions;
using Testcontainers.PostgreSql;

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

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        await WaitForPostgresReady(_postgresContainer.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var testConnectionString = _postgresContainer.GetConnectionString();

        builder.ConfigureServices(services =>
        {
            var descriptorDbConnection = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbConnection));
            if (descriptorDbConnection != null)
                services.Remove(descriptorDbConnection);

            var fmServices = services
                .Where(d =>
                    d.ServiceType.FullName != null && d.ServiceType.FullName.Contains("FluentMigrator")).ToList();

            if (fmServices.Count != 0)
            {
                foreach (var runner in fmServices)
                    services.Remove(runner);
            }

            var hangfireServices = services
                .Where(d => d.ServiceType.Namespace != null && 
                            d.ServiceType.Namespace.StartsWith("Hangfire")).ToList();

            foreach (var descriptor in hangfireServices)
                services.Remove(descriptor);

            var newConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = testConnectionString
                })
                .Build();

            services.AddDatabase(newConfig);
            services.AddHangfireWithPostgres(newConfig);
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            services.AddAuthorization();
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