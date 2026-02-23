using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

public sealed class StubaGetAllCountriesRequest
{
    [JsonPropertyName("Authority")]
    public StubaContentAuthority Authority { get; set; } = new();
}

public sealed class StubaGetAllCountriesResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Data")]
    public List<StubaSearchRegion> Data { get; set; } = new();
}
