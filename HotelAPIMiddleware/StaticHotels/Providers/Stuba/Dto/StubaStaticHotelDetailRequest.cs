using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Request body for POST {ContentBaseUrl}/getAllHotelsDetailsByHotelIds
/// Fetches static detail (images, descriptions, address, etc.) for one or more hotels.
/// Auth is embedded in the body (Authority object), not in headers.
/// </summary>
public sealed class StubaGetHotelDetailsRequest
{
    [JsonPropertyName("Authority")]
    public StubaContentAuthority Authority { get; set; } = new();

    [JsonPropertyName("HotelIds")]
    public List<string> HotelIds { get; set; } = new();

    [JsonPropertyName("Options")]
    public StubaHotelDetailOptions Options { get; set; } = new();
}

public sealed class StubaHotelDetailOptions
{
    [JsonPropertyName("IncludeImages")]
    public bool IncludeImages { get; set; } = true;

    [JsonPropertyName("IncludeDescription")]
    public bool IncludeDescription { get; set; } = true;

    [JsonPropertyName("IncludeFacilities")]
    public bool IncludeFacilities { get; set; } = true;
}
