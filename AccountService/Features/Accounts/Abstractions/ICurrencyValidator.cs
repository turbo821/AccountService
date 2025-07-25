namespace AccountService.Features.Accounts.Abstractions;

public interface ICurrencyValidator
{
    bool IsValid(string currency);
}