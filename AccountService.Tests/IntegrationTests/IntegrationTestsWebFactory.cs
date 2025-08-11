using FluentMigrator.Runner;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AccountService.Features.Accounts;
using Testcontainers.PostgreSql;

namespace AccountService.Tests.IntegrationTests;

public class IntegrationTestsWebFactory : WebApplicationFactory<Program>
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithImage("postgres:17")
        .Build();

    public IntegrationTestsWebFactory()
    {
        _postgresContainer.StartAsync().GetAwaiter().GetResult();
        ApplyMigrations();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(IDbConnection)).ToList();
            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(_postgresContainer.GetConnectionString()));

            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });

            services.AddAuthorization();
        });
    }

    private void ApplyMigrations()
    {
        // Настройка FluentMigrator runner
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(_postgresContainer.GetConnectionString())
                .ScanIn(typeof(Account).Assembly).For.Migrations()
            )
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        runner.MigrateUp();
    }

    public new async ValueTask DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
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