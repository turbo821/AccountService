using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

public record UpdateAccountCommand(Guid AccountId, decimal InterestRate) : IRequest;

public record UpdateInterestRateRequest(decimal InterestRate);