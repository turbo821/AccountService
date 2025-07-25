using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Accounts.GetAccountList;
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
            return Ok(accountId);
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
}
