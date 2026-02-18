using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.Stuba.Dto;

/// <summary>
/// Request body sent to POST /BookingPrepare on the Stuba API.
/// </summary>
public sealed class StubaBookingPrepareRequest
{
    [JsonPropertyName("QuoteId")]
    public string QuoteId { get; set; } = string.Empty;
}
