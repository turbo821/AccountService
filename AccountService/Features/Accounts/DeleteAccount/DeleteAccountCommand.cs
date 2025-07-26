using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public record DeleteAccountCommand(Guid Id) : IRequest<Guid>;