using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Request body for POST {BaseUrl}/GetDestinationHotels
///
/// PLACEHOLDER: Verify field names against official STUBA docs.
/// </summary>
public sealed class StubaStaticRegionHotelsRequest
{
    [JsonPropertyName("DestinationId")]
    public string DestinationId { get; set; } = string.Empty;

    [JsonPropertyName("PageIndex")]
    public int PageIndex { get; set; } = 1;

    [JsonPropertyName("PageSize")]
    public int PageSize { get; set; } = 1000;
}
