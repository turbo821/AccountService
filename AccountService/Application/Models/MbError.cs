using JetBrains.Annotations;

namespace AccountService.Application.Models;

/// <summary>
/// Ошибка с сообщением и дополнительной информацией.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class MbError(string message, IDictionary<string, string[]>? details = null)
{
    /// <summary>
    /// Сообщение ошибки.
    /// </summary>
    public string Message { get; set; } = message;

    /// <summary>
    /// Детали ошибки для отдельных полей.
    /// </summary>
    public IDictionary<string, string[]> Details { get; set; } = details ?? new Dictionary<string, string[]>();
}