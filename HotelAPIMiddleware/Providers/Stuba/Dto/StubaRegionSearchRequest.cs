using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.Stuba.Dto;

public sealed class StubaRegionSearchRequest
{
    [JsonPropertyName("Nationality")]
    public string Nationality { get; set; } = string.Empty; // e.g., "GB"

    [JsonPropertyName("Timeout")]
    public int Timeout { get; set; } = 30;

    [JsonPropertyName("RegionId")]
    public int RegionId { get; set; }

    [JsonPropertyName("ArrivalDate")]
    public string ArrivalDate { get; set; } = string.Empty; // yyyy-MM-dd

    [JsonPropertyName("Nights")]
    public int Nights { get; set; }

    [JsonPropertyName("Rooms")]
    public List<StubaRoom> Rooms { get; set; } = new();
}

public sealed class StubaRoom
{
    [JsonPropertyName("Adult")]
    public int Adult { get; set; }

    // Stuba sometimes supports Child/Ages depending on API version.
    // Keep optional to extend later.
    [JsonPropertyName("Child")]
    public int? Child { get; set; }

    [JsonPropertyName("ChildAges")]
    public List<int>? ChildAges { get; set; }
}
