using Microsoft.AspNetCore.Mvc;
using AccountService.Application.Abstractions;

namespace AccountService.Controllers;

/// <summary>
/// Контроллер для проверки состояния RabbitMQ.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController(
    IRabbitMqHealthChecker rabbitMqHealth,
    IOutboxRepository outboxRepo,
    ILogger<HealthController> logger)
    : ControllerBase
{
    /// <summary>
    /// Проверка подключения к RabbitMQ.
    /// </summary>
    /// <returns>
    /// Статус подключения к RabbitMQ.
    /// </returns>
    /// <response code="200">RabbitMQ доступен.</response>
    /// <response code="503">RabbitMQ недоступен.</response>

    [HttpGet("live")]
    public async Task<IActionResult> Live(CancellationToken cancellationToken)
    {
        var alive = await rabbitMqHealth.IsAliveAsync(cancellationToken);

        if (alive)
            return Ok(new { Available = true });

        logger.LogWarning("Проверка живости не пройдена: RabbitMQ недоступен");
        return StatusCode(503, new { Available = false, Error = "RabbitMQ недоступен" });
    }

    /// <summary>
    /// Проверка отставания Outbox и RabbitMQ.
    /// </summary>
    /// <returns>Статус готовности приложения с учётом очереди Outbox и подключения к RabbitMQ.</returns>
    /// <response code="200">RabbitMQ доступен, количество неопубликованных сообщений в Outbox в норме.</response>
    /// <response code="429">Количество неопубликованных сообщений в Outbox превышает порог, предупреждение.</response>
    /// <response code="503">RabbitMQ недоступен, приложение не готово.</response>
    /// <response code="500">Произошла непредвиденная ошибка при проверке готовности.</response>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var alive = await rabbitMqHealth.IsAliveAsync(cancellationToken);
            var pendingCount = await outboxRepo.GetPendingCountAsync();

            if (!alive)
            {
                logger.LogWarning("Проверка готовности не пройдена: RabbitMQ недоступен");
                return StatusCode(503, new { Status = "Unhealthy", PendingOutboxMessages = pendingCount, Error = "RabbitMQ недоступен" });
            }

            var status = pendingCount > 100 ? "WARN" : "Healthy";
            return status == "Healthy"
                ? Ok(new { Status = status, PendingOutboxMessages = pendingCount })
                : StatusCode(429, new { Status = status, PendingOutboxMessages = pendingCount, Error = "Высокое количество сообщений в Outbox" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Проверка готовности не пройдена");
            return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
        }
    }
}