using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Response from POST {BaseUrl}/GetHotelList
///
/// PLACEHOLDER: Verify field names against official STUBA docs.
/// Adjust JsonPropertyName values to match the actual API response.
/// </summary>
public sealed class StubaStaticHotelListResponse
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

public sealed class StubaHotelListItem
{
    /// <summary>PLACEHOLDER field name — adjust to match actual STUBA response.</summary>
    [JsonPropertyName("HotelId")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("HotelName")]
    public string HotelName { get; set; } = string.Empty;
}
