using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public record CreateAccountCommand(
    Guid OwnerId, 
    AccountType Type, 
    string Currency, 
    decimal? InterestRate) : IRequest<Guid>;