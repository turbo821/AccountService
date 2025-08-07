using AccountService.Application.Models;

namespace AccountService.Application.Abstractions;

public interface IAuthService
{
    Task<MbResult<AccessTokenResponse>> GetAccessTokenAsync();
}
