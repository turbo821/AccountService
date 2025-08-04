using AccountService.Application.Models;
using System.Text.Json;

namespace AccountService.Application
{
    public interface IAuthService
    {
        Task<MbResult<JsonElement>> GetAccessTokenAsync();
    }
}
