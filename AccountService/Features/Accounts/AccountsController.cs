using AccountService.Features.Accounts.GetAccounts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Features.Accounts;

[ApiController]
[Route("/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator medialor)
    {
        _mediator = medialor;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts([FromQuery] Guid? ownerId = null)
    {
        var query = new GetAccountsQuery(ownerId);
        var accounts = await _mediator.Send(query);
        return Ok(accounts);
    }
}
