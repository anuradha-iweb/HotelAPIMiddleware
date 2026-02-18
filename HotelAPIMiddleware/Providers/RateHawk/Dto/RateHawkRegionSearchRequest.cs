using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.RateHawk.Dto;

public sealed class RateHawkRegionSearchRequest
{
    [JsonPropertyName("checkin")]
    public string CheckIn { get; set; } = string.Empty;  // yyyy-MM-dd

    [JsonPropertyName("checkout")]
    public string CheckOut { get; set; } = string.Empty; // yyyy-MM-dd

    [JsonPropertyName("residency")]
    public string Residency { get; set; } = string.Empty; // e.g., "ae"

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("guests")]
    public List<RateHawkGuest> Guests { get; set; } = new();

    [JsonPropertyName("region_id")]
    public int RegionId { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
}

public sealed class RateHawkGuest
{
    [JsonPropertyName("adults")]
    public int Adults { get; set; }

    // RateHawk uses "children": [age1, age2...]
    [JsonPropertyName("children")]
    public List<int> Children { get; set; } = new();
}
