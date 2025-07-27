using AccountService.Features.Accounts.CheckOwnerAccounts;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.DeleteAccount;
using AccountService.Features.Accounts.GetAccountById;
using AccountService.Features.Accounts.GetAccountList;
using AccountService.Features.Accounts.GetAccountTransactions;
using AccountService.Features.Accounts.RegisterTransaction;
using AccountService.Features.Accounts.TransferBetweenAccounts;
using AccountService.Features.Accounts.UpdateAccount;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Accounts;

[ApiController]
[Route("/accounts")]
public class AccountsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountCommand request)
    {
        var accountId = await mediator.Send(request);
        return Ok(new { AccountId = accountId });
    }

    [HttpGet]
    public async Task<IActionResult> GetAccountList()
    {
        var query = new GetAccountListQuery();
        var accounts = await mediator.Send(query);
        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        var query = new GetAccountByIdQuery(id);
        var account = await mediator.Send(query);
        return Ok(account);
    }

    [HttpPatch("{id:guid}/interest-rate")]
    public async Task<IActionResult> UpdateAccountInterestRate(Guid id, [FromBody] UpdateInterestRateRequest request)
    {
        var command = new UpdateAccountCommand(id, request.InterestRate);
        await mediator.Send(command);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        var command = new DeleteAccountCommand(id);
        var accountId = await mediator.Send(command);
        return Ok(new { AccountId = accountId });
    }

    [HttpPost("{accountId:guid}/transactions")]
    public async Task<IActionResult> RegisterTransaction(Guid accountId, RegisterTransactionRequest request)
    {
        var command = new RegisterTransactionCommand(
            accountId,
            request.Amount,
            request.Currency,
            request.Type,
            request.Description
        );
        var transactionId = await mediator.Send(command);
        return Ok(new { TransactionId = transactionId });
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> TransferBetweenAccounts(TransferBetweenAccountsCommand command)
    {
        await mediator.Send(command);
        return Ok();
    }

    [HttpGet("{accountId:guid}/transactions")]
    public async Task<ActionResult<AccountTransactionsDto>> GetStatement(Guid accountId)
    {
        var result = await mediator.Send(new GetAccountTransactionsQuery(accountId));
        return Ok(result);
    }

    [HttpGet("/owner/{ownerId:guid}")]
    public async Task<IActionResult> CheckOwnerAccounts(Guid ownerId)
    {
        var result = await mediator.Send(new CheckOwnerAccountsQuery(ownerId));
        return Ok(result);
    }
}
