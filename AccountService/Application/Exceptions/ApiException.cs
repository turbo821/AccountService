using System.Net;

namespace AccountService.Application.Exceptions;

public class ApiException(HttpStatusCode statusCode, string message, IDictionary<string, string[]>? errors = null)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public IDictionary<string, string[]>? Errors { get; } = errors ?? new Dictionary<string, string[]>();
}