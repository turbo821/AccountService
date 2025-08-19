namespace AccountService.Application.Abstractions;

public interface IRabbitMqHealthChecker
{
    Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);
}