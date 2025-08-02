using MediatR;
using System.Net;
using AccountService.Application.Exceptions;
using FluentValidation;

namespace AccountService.Application.Middlewares;

public class ExceptionBehavior<TRequest, TResponse>(
    ILogger<ExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Not found: {Request}", typeof(TRequest).Name);
            throw new ApiException(HttpStatusCode.NotFound, ex.Message);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed: {Request}", typeof(TRequest).Name);
            var errorDict = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            throw new ApiException(HttpStatusCode.BadRequest, "Validation failed", errorDict);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation failed: {Request}", typeof(TRequest).Name);
            throw new ApiException(HttpStatusCode.BadRequest, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Validation failed: {Request}", typeof(TRequest).Name);
            throw new ApiException(HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error for {Request}", typeof(TRequest).Name);
            throw new ApiException(HttpStatusCode.InternalServerError, "An error occurred while processing your request");
        }
    }
}