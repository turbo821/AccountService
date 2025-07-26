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
        try
        {
            var accountId = await mediator.Send(request);
            return Ok(new { AccountId = accountId });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
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
        try
        {
            var query = new GetAccountByIdQuery(id);
            var account = await mediator.Send(query);
            return Ok(account);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public record UpdateInterestRateRequest(decimal InterestRate);

    [HttpPatch("{id:guid}/interest-rate")]
    public async Task<IActionResult> UpdateAccountInterestRate(Guid id, [FromBody] UpdateInterestRateRequest request)
    {
        try
        {
            var command = new UpdateAccountCommand(id, request.InterestRate);
            await mediator.Send(command);
            return Ok();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        try
        {
            var command = new DeleteAccountCommand(id);
            var accountId = await mediator.Send(command);
            return Ok(new { AccountId = accountId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    public record RegisterTransactionRequest(
        decimal Amount,
        string Currency,
        TransactionType Type,
        string Description
    );

    [HttpPost("{accountId}/transactions")]
    public async Task<IActionResult> RegisterTransaction(Guid accountId, RegisterTransactionRequest request)
    {
        var command = new RegisterTransactionCommand(
            accountId,
            request.Amount,
            request.Currency,
            request.Type,
            request.Description
        );
        try
        {
            var transactionId = await mediator.Send(command);
            return Ok(new { TransactionId = transactionId });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> TransferBetweenAccounts(TransferBetweenAccountsCommand command)
    {
        try
        {
            await mediator.Send(command);
            return Ok();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("{accountId:guid}/transactions")]
    public async Task<ActionResult<AccountTransactionsDto>> GetStatement(Guid accountId)
    {
        try
        {
            var result = await mediator.Send(new GetAccountTransactionsQuery(accountId));
            return Ok(result);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("/owner/{ownerId:guid}")]
    public async Task<IActionResult> CheckOwnerAccounts(Guid ownerId)
    {
        try
        {
            var result = await mediator.Send(new CheckOwnerAccountsQuery(ownerId));
            return Ok(result);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}
