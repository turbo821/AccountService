using AccountService.Background;
using FluentMigrator.Runner;
using Hangfire;
using Hangfire.Dashboard;
using RabbitMQ.Client;

namespace AccountService.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication RunMigrations(this WebApplication app)
    {
        var isDatabaseReady = false;
        var retries = 0;
        const int maxRetries = 10;
        const int delayMs = 2000;

        using var scope = app.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        while (!isDatabaseReady && retries < maxRetries)
        {
            try
            {
                if (runner.HasMigrationsToApplyUp())
                {
                    Console.WriteLine("Applying pending migrations...");
                    runner.MigrateUp();
                }
                else
                {
                    Console.WriteLine("No pending migrations.");
                }
                isDatabaseReady = true;
            }
            catch (Exception ex)
            {
                retries++;
                Console.WriteLine($"Database not ready yet (attempt {retries}/{maxRetries}). Waiting {delayMs}ms... Error: {ex.Message}");
                Thread.Sleep(delayMs);
            }
        }

        if (!isDatabaseReady)
        {
            throw new Exception("Failed to connect to database after multiple attempts");
        }

        return app;
    }

    public static WebApplication UseHangfire(this WebApplication app)
    {
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var storage = scope.ServiceProvider.GetService<JobStorage>();
                if (storage is null)
                {
                    app.Logger.LogWarning("Hangfire storage not configured. Skipping Hangfire setup.");
                    return app;
                }
            }

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireAuthorizationFilter()]
            });

            RecurringJob.AddOrUpdate<InterestAccrualHandler>(
                "accrue-interest-daily",
                s => s.AccrueDailyInterestAsync(),
                Cron.Daily
            );

            RecurringJob.AddOrUpdate<OutboxDispatcher>(
                "outbox-processor",
                processor => processor.ProcessOutboxMessages(),
                "*/10 * * * * *");
            
            app.Logger.LogInformation("Hangfire successfully configured.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Hangfire setup failed. Hangfire will not be used.");
        }

        return app;
    }

    public static async Task InitializeRabbitMqAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IConnectionFactory>();

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "account.events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );

        var queues = new[]
        {
            "account.crm",
            "account.notifications",
            "account.antifraud",
            "account.audit"
        };

        foreach (var queue in queues)
        {
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );
        }

        await channel.QueueBindAsync("account.crm", "account.events", "account.*");
        await channel.QueueBindAsync("account.notifications", "account.events", "money.*");
        await channel.QueueBindAsync("account.antifraud", "account.events", "client.*");
        await channel.QueueBindAsync("account.audit", "account.events", "#");
    }
}


public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}