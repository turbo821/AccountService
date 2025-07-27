using AccountService.Features.Accounts.Abstractions;

namespace AccountService.Infrastructure.Services;

public class CurrencyValidatorStub : ICurrencyValidator
{
    private static readonly HashSet<string> SupportedCurrencies = ["USD", "EUR", "RUB", "KZT"];

    public bool IsExists(string currency)
        => SupportedCurrencies.Contains(currency.ToUpperInvariant());
}