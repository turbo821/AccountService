using System.Net;
using AccountService.Application.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using AccountService.Application.Exceptions;
using AccountService.Application.Abstractions;

namespace AccountService.Infrastructure.Services;

public class AuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IAuthService
{
    public async Task<MbResult<AccessTokenResponse>> GetAccessTokenAsync()
    {
        var httpClient = httpClientFactory.CreateClient();

        var clientId = configuration["Keycloak:ClientId"]!;
        var tokenUrl = configuration["Keycloak:TokenUrl"]!;
        var username = configuration["TestUser:Username"]!;
        var password = configuration["TestUser:Password"]!;

        if (string.IsNullOrEmpty(tokenUrl))
            throw new ApiException(HttpStatusCode.InternalServerError, "Keycloak:TokenUrl is not set in configuration");

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", clientId },
            { "username", username },
            { "password", password }
        };

        var content = new FormUrlEncodedContent(parameters);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        var response = await httpClient.PostAsync(tokenUrl, content);

        if (!response.IsSuccessStatusCode)
            throw new ApiException(HttpStatusCode.Unauthorized,
                "Failed to retrieve access token. Incorrect credentials");

        var json = await response.Content.ReadAsStringAsync();
        
        using var jsonDoc = JsonDocument.Parse(json);
        var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
        var result = new AccessTokenResponse(accessToken!);

        return new MbResult<AccessTokenResponse>(result);
    }
}