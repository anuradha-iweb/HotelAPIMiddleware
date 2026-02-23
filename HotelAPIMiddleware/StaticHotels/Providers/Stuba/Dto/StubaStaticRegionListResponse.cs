using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Response from POST {BaseUrl}/GetDestinationList
///
/// PLACEHOLDER: Verify field names against official STUBA docs.
/// </summary>
public sealed class StubaStaticRegionListResponse
{
    [JsonPropertyName("Destinations")]
    public List<StubaDestination> Destinations { get; set; } = new();
}

public sealed class StubaDestination
{
    [JsonPropertyName("DestinationId")]
    public string DestinationId { get; set; } = string.Empty;

    [JsonPropertyName("DestinationName")]
    public string DestinationName { get; set; } = string.Empty;

    [JsonPropertyName("CountryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("HotelCount")]
    public int HotelCount { get; set; }
}
