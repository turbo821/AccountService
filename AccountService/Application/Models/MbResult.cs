namespace AccountService.Application.Models;

/// <summary>
/// Результат выполнения операции с данными.
/// </summary>
public class MbResult<T>
{
    /// <summary>
    /// Успешность выполнения операции.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Данные, возвращаемые в случае успешного выполнения операции.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Ошибка, если операция не удалась.
    /// </summary>
    public MbError? Error { get; set; }

    public MbResult(T data)
    {
        Success = true;
        Data = data;
        Error = null;
    }

    public MbResult(MbError error)
    {
        Success = false;
        Data = default;
        Error = error;
    }

    public MbResult()
    {
    }
}