using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Responses;

public class ProviderSearchResult
{
    public HotelProvider Provider { get; set; }

    public bool Success { get; set; }

    public List<HotelResult> Hotels { get; set; } = new();

    public string? ErrorMessage { get; set; }
}
