namespace AccountService.Application.Abstractions;

public interface IConsumerHandler
{
    Task HandleAsync(byte[] body);
}