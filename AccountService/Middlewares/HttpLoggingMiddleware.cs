using System.Diagnostics;
using JetBrains.Annotations;

namespace AccountService.Middlewares;

public class HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
{
    [UsedImplicitly]
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        var request = context.Request;
        var requestInfo = $"{request.Method} {request.Path}{request.QueryString}";

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();

            logger.LogInformation(
                "HTTP {Request} responded {StatusCode} in {Elapsed}ms",
                requestInfo,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds
            );
        }
    }
}