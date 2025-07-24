using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(IMapper mapper) : IRequestHandler<CreateAccountCommand, Guid>
{
    public Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = mapper.Map<Account>(request);

        return Task.FromResult(account.Id);
    }
}