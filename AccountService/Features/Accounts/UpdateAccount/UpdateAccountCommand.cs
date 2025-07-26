using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

public record UpdateAccountCommand(Guid Id, decimal InterestRate) : IRequest; 