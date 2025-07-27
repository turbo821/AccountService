namespace AccountService.Features.Accounts.Abstractions;

public interface ICurrencyValidator
{
    bool IsExists(string currency);
}