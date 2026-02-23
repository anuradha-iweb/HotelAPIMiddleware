using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Request body for POST {BaseUrl}/GetHotelContent
///
/// PLACEHOLDER: Verify field names against official STUBA docs.
/// Some STUBA versions accept a list of IDs; adjust if needed.
/// </summary>
public sealed class StubaStaticHotelDetailRequest
{
    [JsonPropertyName("HotelId")]
    public string HotelId { get; set; } = string.Empty;
}
