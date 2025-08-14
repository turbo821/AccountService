using AccountService.Features.Accounts.Abstractions;
using FluentMigrator.Runner;
using Hangfire;
using Hangfire.Dashboard;

namespace AccountService.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication RunMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        if (runner.HasMigrationsToApplyUp())
        {
            Console.WriteLine("Applying pending migrations...");
            runner.MigrateUp();
        }
        else
        {
            Console.WriteLine("No pending migrations.");
        }

        return app;
    }

    public static WebApplication UseHangfire(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()]
        });

        RecurringJob.AddOrUpdate<IInterestAccrualService>(
            "accrue-interest-daily",
            s => s.AccrueDailyInterestAsync(),
            Cron.Daily
        );

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