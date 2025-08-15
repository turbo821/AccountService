using AccountService.Background;
using FluentMigrator.Runner;
using Hangfire;
using Hangfire.Dashboard;

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
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()]
        });

        RecurringJob.AddOrUpdate<InterestAccrualHandler>(
            "accrue-interest-daily",
            s => s.AccrueDailyInterestAsync(),
            Cron.Daily
        );
        RecurringJob.AddOrUpdate<OutboxProcessor>(
            "outbox-processor",
            processor => processor.ProcessOutboxMessages(),
            Cron.Minutely);
            
        return app;
    }
}

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}