using AccountService.Application.Abstractions;
using RabbitMQ.Client;

namespace AccountService.Infrastructure.Services;

public class RabbitMqHealthCheck(IConnectionFactory connectionFactory, ILogger<RabbitMqHealthCheck> logger)
    : IRabbitMqHealthCheck
{
    public async Task<bool> IsAliveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            var isOpen = channel.IsOpen;
            logger.LogInformation("RabbitMQ connection is {Status}", isOpen ? "open" : "closed");
            return isOpen;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check RabbitMQ health");
            return false;
        }
    }
}