namespace AccountService.Features.Accounts;

public record AccountDto(
    Guid Id,
    Guid OwnerId,
    AccountType Type,
    string Currency,
    decimal Balance,
    decimal? InterestRate,
    DateTime OpenedAt);