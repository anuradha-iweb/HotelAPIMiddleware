using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.RateHawk.Dto;

public sealed class RateHawkHotelPageRequest
{
    [JsonPropertyName("checkin")]
    public string CheckIn { get; set; } = string.Empty;

    [JsonPropertyName("checkout")]
    public string CheckOut { get; set; } = string.Empty;

    [JsonPropertyName("residency")]
    public string Residency { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("guests")]
    public List<RateHawkGuest> Guests { get; set; } = new();

    [JsonPropertyName("hid")]
    public long Hid { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30;
}