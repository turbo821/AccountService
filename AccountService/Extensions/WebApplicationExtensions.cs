using FluentMigrator.Runner;

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

}