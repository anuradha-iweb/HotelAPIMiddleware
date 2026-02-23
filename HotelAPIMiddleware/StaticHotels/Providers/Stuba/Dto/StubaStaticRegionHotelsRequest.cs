using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Request body for POST {ContentBaseUrl}/getAllSearchRegionsByCountry
/// Used for both fetching top-level regions and cities within a region.
/// Auth is embedded in the body (Authority object), not in headers.
/// </summary>
public sealed class StubaGetSearchRegionsRequest
{
    [JsonPropertyName("Authority")]
    public StubaContentAuthority Authority { get; set; } = new();

    [JsonPropertyName("RegionId")]
    public int RegionId { get; set; }
}

public sealed class StubaContentAuthority
{
    [JsonPropertyName("Org")]
    public string Org { get; set; } = string.Empty;

    [JsonPropertyName("User")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("Password")]
    public string Password { get; set; } = string.Empty;
}
