namespace HotelAPIMiddleware.Contracts.Responses;

public class ProviderExecutionInfo
{
    public string Provider { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int TimeMs { get; set; }
    public string? Error { get; set; }
}
