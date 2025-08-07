namespace AccountService.Application.Models;

/// <summary>
/// Ответ с токеном доступа
/// </summary>
/// <param name="AccessToken">Токен доступа</param>
public record AccessTokenResponse(string AccessToken);