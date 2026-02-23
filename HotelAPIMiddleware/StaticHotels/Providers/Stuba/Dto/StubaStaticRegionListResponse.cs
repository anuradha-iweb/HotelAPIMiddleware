using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Response from POST {ContentBaseUrl}/getAllSearchRegionsByCountry
/// when the regionId resolves to a country-level node.
/// Data items contain RegionId / RegionName.
/// </summary>
public sealed class StubaGetRegionsResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Data")]
    public List<StubaSearchRegion> Data { get; set; } = new();
}

public sealed class StubaSearchRegion
{
    [JsonPropertyName("RegionId")]
    public int RegionId { get; set; }

    [JsonPropertyName("RegionName")]
    public string RegionName { get; set; } = string.Empty;
}
