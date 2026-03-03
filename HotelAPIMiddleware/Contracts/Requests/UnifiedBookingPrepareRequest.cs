using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Requests;

public class UnifiedBookingPrepareRequest
{
    public HotelProvider Provider { get; set; }

    // Provider-specific payload:
    // Stuba     -> { "QuoteId": "..." }
    // RateHawk  -> { "RefNo"/"Hid": "...", "CheckIn": "...", ... }
    public JsonElement Data { get; set; }
}
