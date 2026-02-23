using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.Stuba.StaticContent.Dto;

public sealed class StubaContentAuthority
{
    [JsonPropertyName("Org")]
    public string Org { get; set; } = string.Empty;

    [JsonPropertyName("User")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("Password")]
    public string Password { get; set; } = string.Empty;
}
