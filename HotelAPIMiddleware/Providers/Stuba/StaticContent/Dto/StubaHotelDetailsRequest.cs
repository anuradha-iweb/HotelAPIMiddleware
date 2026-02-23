using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.Stuba.StaticContent.Dto;

/// <summary>
/// Request body for POST /webapi/staticData/getAllHotelsDetailsByHotelIds
/// Stuba API accepts a maximum of 100 hotel IDs per call.
/// </summary>
public sealed class StubaHotelDetailsRequest
{
    [JsonPropertyName("Authority")]
    public StubaContentAuthority Authority { get; set; } = new();

    [JsonPropertyName("HotelIds")]
    public List<int> HotelIds { get; set; } = new();

    [JsonPropertyName("Options")]
    public StubaHotelDetailsOptions Options { get; set; } = new();
}

public sealed class StubaHotelDetailsOptions
{
    [JsonPropertyName("IncludeImages")]
    public bool IncludeImages { get; set; } = true;

    [JsonPropertyName("IncludeDescription")]
    public bool IncludeDescription { get; set; } = true;
}
