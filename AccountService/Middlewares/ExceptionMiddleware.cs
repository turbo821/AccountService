using AccountService.Exceptions;
using JetBrains.Annotations;
using System.Text.Json;

namespace AccountService.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    [UsedImplicitly]
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)ex.StatusCode;

            var result = JsonSerializer.Serialize(new ErrorResponse
            {
                Message = ex.Message,
                Errors = ex.Errors.Any() ? ex.Errors : null
            });

            await context.Response.WriteAsync(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error");

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse { Message = ex.Message }));
        }
    }
}

/// <summary>
/// Ответ при ошибке
/// </summary>
public class ErrorResponse
{
    public required string Message { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}