using AccountService.Application;
using AccountService.Application.Models;
using System.Net.Http;
using System.Text.Json;

namespace AccountService.Infrastructure.Services;

public class AuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IAuthService
{
    public async Task<MbResult<JsonElement>> GetAccessTokenAsync()
    {
        var httpClient = httpClientFactory.CreateClient();

        var clientId = configuration["Keycloak:ClientId"]!;
        var tokenUrl = configuration["Keycloak:TokenUrl"]!;
        var username = configuration["TestUser:Username"]!;
        var password = configuration["TestUser:Password"]!;

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", clientId },
            // { "scope", openid }
            { "username", username },
            { "password", password }
        };

        var content = new FormUrlEncodedContent(parameters);
        var response = await httpClient.PostAsync(tokenUrl, content);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Failed to retrieve access token. Status code: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        return new MbResult<JsonElement>(result);
    }
}