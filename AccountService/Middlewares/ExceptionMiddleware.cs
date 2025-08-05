using System.Net;
using AccountService.Application.Exceptions;
using JetBrains.Annotations;
using System.Text.Json;
using AccountService.Application.Models;

namespace AccountService.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    [UsedImplicitly]
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);

            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                throw new ApiException(HttpStatusCode.Unauthorized, 
                    "Unauthorized, user don't have access rights to the requested resource");
        }
        catch (ApiException ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)ex.StatusCode;

            var error = new MbError(ex.Message, ex.Errors);

            var result = JsonSerializer.Serialize(new MbResult<object>(error));
            await context.Response.WriteAsync(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error");

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var error = new MbError(ex.Message);
            var result = JsonSerializer.Serialize(new MbResult<object>(error));

            await context.Response.WriteAsync(result);
        }
    }
}