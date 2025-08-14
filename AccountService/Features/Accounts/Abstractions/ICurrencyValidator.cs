namespace AccountService.Features.Accounts.Abstractions;

public interface ICurrencyValidator
{
    Task<bool> IsExists(string currency);
}