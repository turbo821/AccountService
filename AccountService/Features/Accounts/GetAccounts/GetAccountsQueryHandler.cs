namespace AccountService.Features.Accounts.GetAccounts;

public class GetAccountsQueryHandler
{
    // private readonly IAccountRepository _accountRepository;
    public GetAccountsQueryHandler()
    {
        
    }
    public async Task<object?> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        //var accounts = await _accountRepository.GetAccountsAsync(request.OwnerId, cancellationToken);
        //return accounts.Select(account => new
        //{
        //    account.Id,
        //    account.OwnerId,
        //    account.AccountType,
        //    account.Balance,
        //    account.CreatedAt,
        //    account.UpdatedAt
        //});
        throw new NotImplementedException("This method is not implemented yet.");
    }
}