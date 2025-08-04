using AccountService.Application;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpGet("token")]
    public async Task<IActionResult> GetToken()
    {
        var token = await authService.GetAccessTokenAsync();

        return Ok(new { access_token = token });
    }
}