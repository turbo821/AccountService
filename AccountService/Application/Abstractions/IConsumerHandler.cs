namespace AccountService.Application.Abstractions;

public interface IConsumerHandler
{
    Task HandleAsync(string eventJson, string eventType);
}