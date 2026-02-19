namespace HotelAPIMiddleware.Contracts.Responses;

public class HotelSearchResponse
{
    public string SearchId { get; set; } = Guid.NewGuid().ToString();

    public string CacheId { get; set; } = string.Empty;

    public List<HotelResult> Hotels { get; set; } = new();

    public List<ProviderExecutionInfo> Providers { get; set; } = new();
}
