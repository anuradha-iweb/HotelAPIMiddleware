namespace HotelAPIMiddleware.Common;

/// <summary>
/// Unified API response envelope used across all endpoints.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = new();
    public string TraceId { get; init; } = Guid.NewGuid().ToString();

    public static ApiResponse<T> Ok(T data, string message = "OK") => new()
    {
        Success = true,
        Message = message,
        Data = data,
        TraceId = Guid.NewGuid().ToString()
    };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        Errors = errors?.ToList() ?? new List<string>(),
        TraceId = Guid.NewGuid().ToString()
    };
}
