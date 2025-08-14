using AccountService.Application.Abstractions;
using AccountService.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Получение token доступа (используются данные тестового пользователя)
    /// </summary>
    /// <remarks>
    /// Используемые данные:
    /// <ul>
    ///     <li>client_id: account-api</li>
    ///     <li>username: tom</li>
    ///     <li>password: pass123</li>
    /// </ul>
    /// </remarks>
    /// <returns>Данные с AccessToken</returns>
    /// <response code="200">Token доступа получен</response>
    /// <response code="401">Сервер аутентификации недоступен или данные неверны</response>
    [HttpGet("token")]
    [ProducesResponseType(typeof(MbResult<AccessTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MbResult<Unit>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetToken()
    {
        var response = await authService.GetAccessTokenAsync();

        return Ok(response);
    }
}