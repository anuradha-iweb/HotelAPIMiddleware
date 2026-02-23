using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Response from POST {BaseUrl}/GetDestinationHotels
///
/// PLACEHOLDER: Verify field names against official STUBA docs.
/// </summary>
public sealed class StubaStaticRegionHotelsResponse
{
    [JsonPropertyName("TotalRecord")]
    public int TotalRecord { get; set; }

    [JsonPropertyName("CurrentPage")]
    public int CurrentPage { get; set; }

    [JsonPropertyName("PageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("HotelList")]
    public List<StubaHotelListItem> HotelList { get; set; } = new();
}
