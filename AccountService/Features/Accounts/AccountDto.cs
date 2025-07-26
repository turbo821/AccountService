namespace AccountService.Features.Accounts;

public record AccountDto(
    Guid Id,
    AccountType Type,
    string Currency,
    decimal Balance,
    decimal? InterestRate);