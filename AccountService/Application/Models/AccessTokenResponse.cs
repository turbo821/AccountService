namespace AccountService.Application.Models;

/// <summary>
/// Ответ с token доступа
/// </summary>
/// <param name="AccessToken">Token доступа</param>
public record AccessTokenResponse(string AccessToken);