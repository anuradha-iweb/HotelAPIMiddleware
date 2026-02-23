using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Response from POST {ContentBaseUrl}/getAllSearchRegionsByCountry
/// when the regionId resolves to a region node that contains cities.
/// Data items contain CityId / CityName / CityWiseRegionList.
/// </summary>
public sealed class StubaGetCitiesResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Data")]
    public List<StubaSearchCity> Data { get; set; } = new();
}

public sealed class StubaSearchCity
{
    [JsonPropertyName("CityId")]
    public int CityId { get; set; }

    [JsonPropertyName("CityName")]
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    /// Search region IDs to use for RegionSearch availability calls.
    /// May be null if the city maps directly to a single search region.
    /// </summary>
    [JsonPropertyName("CityWiseRegionList")]
    public List<StubaSearchRegion>? CityWiseRegionList { get; set; }
}
