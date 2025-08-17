namespace AccountService.Application.Abstractions;

public interface IRabbitMqHealthCheck
{
    Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);
}