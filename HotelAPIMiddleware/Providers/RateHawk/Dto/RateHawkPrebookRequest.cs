using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.RateHawk.Dto;

/// <summary>
/// Request body sent to POST /hotel/prebook/ on the RateHawk API.
/// The hash must be a book_hash (h- prefix) obtained from /search/hp/.
/// The response will contain a new book_hash with a p- prefix that is
/// used for the actual booking step.
/// </summary>
public sealed class RateHawkPrebookRequest
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    [JsonPropertyName("price_increase_percent")]
    public int PriceIncreasePercent { get; set; } = 20;
}
