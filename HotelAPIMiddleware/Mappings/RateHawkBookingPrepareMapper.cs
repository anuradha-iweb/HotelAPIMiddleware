using System.Text.Json;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Mappings;

/// <summary>
/// Helpers to work with RateHawk HP-search and /hotel/prebook/ responses
/// for the unified booking-prepare flow.
///
/// Flow:
///   Step 1  POST /search/hp/   → returns hotels[].rates[] each with book_hash (h- prefix)
///   Step 2  POST /hotel/prebook/ → confirms price; returns book_hash (p- prefix) for actual booking
/// </summary>
public static class RateHawkBookingPrepareMapper
{
    /// <summary>
    /// Extracts the book_hash to use for prebook from the /search/hp/ response.
    ///
    /// If <paramref name="preferredBookHash"/> is provided the method tries to
    /// locate a rate whose book_hash matches; otherwise the first available rate
    /// is returned.
    /// </summary>
    /// <returns>The book_hash string, or null when not found.</returns>
    public static string? ExtractBookHashFromHp(string hpJson, string? preferredBookHash)
    {
        using var doc = JsonDocument.Parse(hpJson);
        var root = doc.RootElement;

        var hotels = root.TryGet("data")?.TryGet("hotels");
        if (hotels is null) return null;

        foreach (var hotel in hotels.Value.EnumerateArraySafe())
        {
            var rates = hotel.TryGet("rates");
            if (rates is null) continue;

            foreach (var rate in rates.Value.EnumerateArraySafe())
            {
                var bh = rate.TryGet("book_hash").GetStringSafe();
                if (string.IsNullOrWhiteSpace(bh)) continue;

                // If caller specified a preferred hash, try to match it exactly
                if (!string.IsNullOrWhiteSpace(preferredBookHash))
                {
                    if (string.Equals(bh, preferredBookHash, StringComparison.OrdinalIgnoreCase))
                        return bh;
                }
                else
                {
                    // No preference – return first available hash
                    return bh;
                }
            }
        }

        // Preferred hash not found in HP response → fall back to provided hash
        return preferredBookHash;
    }

    /// <summary>
    /// Maps the /hotel/prebook/ response JSON to the unified
    /// <see cref="BookingPrepareResponse"/>.
    ///
    /// Prebook response shape:
    /// {
    ///   "data": {
    ///     "hotels": [{
    ///       "id":  "seasun_hostel",
    ///       "hid": 13649126,
    ///       "rates": [{
    ///         "book_hash":   "p-92c0dfe9-...",
    ///         "match_hash":  "m-78006494-...",
    ///         "daily_prices": ["43.00"],
    ///         "meal":        "nomeal",
    ///         "room_name":   "Bed in Dorm (shared bathroom)",
    ///         "payment_options": { "payment_types": [{ "show_amount": "43.00",
    ///             "show_currency_code": "USD", "type": "deposit",
    ///             "tax_data": { "taxes": [...] },
    ///             "cancellation_penalties": {
    ///               "policies": [...],
    ///               "free_cancellation_before": null } }] }
    ///       }]
    ///     }],
    ///     "changes": { "price_changed": false }
    ///   },
    ///   "status": "ok"
    /// }
    /// </summary>
    public static BookingPrepareResponse MapFromPrebookJson(
        string prebookJson,
        string refNo,
        string checkIn,
        string checkOut)
    {
        using var doc = JsonDocument.Parse(prebookJson);
        var root = doc.RootElement;

        var apiStatus = root.TryGet("status").GetStringSafe() ?? "ok";

        if (!string.Equals(apiStatus, "ok", StringComparison.OrdinalIgnoreCase))
        {
            return new BookingPrepareResponse
            {
                Provider    = "RATEHAWK",
                RefNo       = refNo,
                CommitLevel = "prebook",
                Status      = apiStatus,
                Error       = root.TryGet("error").GetStringSafe() ?? "Unknown error from RateHawk prebook"
            };
        }

        var data = root.TryGet("data");

        // price_changed flag
        var priceChanged = data?.TryGet("changes")?.TryGet("price_changed") is JsonElement pcEl
                           && pcEl.ValueKind == JsonValueKind.True;

        var response = new BookingPrepareResponse
        {
            Provider    = "RATEHAWK",
            CommitLevel = "prebook",
            RefNo       = refNo,
            PriceChanged = priceChanged,
            Status      = "ok"
        };

        if (data is null) return response;

        var hotelsEl = data.Value.TryGet("hotels");
        if (hotelsEl is null || hotelsEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        // Use first hotel returned by prebook
        var hotelEl = hotelsEl.Value.EnumerateArray().FirstOrDefault();
        if (hotelEl.ValueKind == JsonValueKind.Undefined) return response;

        var hotelId   = hotelEl.TryGet("hid").GetIntSafe()?.ToString()
                        ?? hotelEl.TryGet("hid").GetStringSafe()
                        ?? hotelEl.TryGet("id").GetStringSafe()
                        ?? string.Empty;
        var hotelName = hotelEl.TryGet("id").GetStringSafe() ?? string.Empty; // slug

        // Derive nights from dates
        var nights = 0;
        if (DateOnly.TryParse(checkIn,  out var ci) &&
            DateOnly.TryParse(checkOut, out var co))
            nights = co.DayNumber - ci.DayNumber;

        response.Hotel = new BookingPrepareHotelInfo
        {
            HotelId   = hotelId,
            HotelName = hotelName,
            CheckIn   = checkIn,
            CheckOut  = checkOut,
            Nights    = nights
        };

        var ratesEl = hotelEl.TryGet("rates");
        if (ratesEl is null || ratesEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        // Use first rate returned by prebook (caller selected a specific hash)
        var rateEl = ratesEl.Value.EnumerateArray().FirstOrDefault();
        if (rateEl.ValueKind == JsonValueKind.Undefined) return response;

        // p- prefixed book_hash for the actual booking call
        var prebookHash = rateEl.TryGet("book_hash").GetStringSafe();
        response.BookHash = prebookHash;

        var roomName   = rateEl.TryGet("room_name").GetStringSafe() ?? string.Empty;
        var meal       = rateEl.TryGet("meal").GetStringSafe()      ?? string.Empty;
        var dailyPrices = rateEl.TryGet("daily_prices").EnumerateArraySafe()
                              .Select(x => x.GetStringSafe() ?? "")
                              .ToList();

        // Derive total from show_amount (display currency) in first payment type
        var paymentTypesEl = rateEl.TryGet("payment_options")?.TryGet("payment_types");
        var firstPt        = paymentTypesEl?.EnumerateArraySafe().FirstOrDefault()
                             ?? default;

        var showAmount   = firstPt.ValueKind != JsonValueKind.Undefined
            ? firstPt.TryGet("show_amount").GetDecimalSafe() ?? 0m
            : 0m;
        var showCurrency = firstPt.ValueKind != JsonValueKind.Undefined
            ? firstPt.TryGet("show_currency_code").GetStringSafe() ?? "USD"
            : "USD";
        var paymentType  = firstPt.ValueKind != JsonValueKind.Undefined
            ? firstPt.TryGet("type").GetStringSafe() ?? string.Empty
            : string.Empty;

        response.Currency = showCurrency;

        // Taxes
        var taxes = new List<BookingPrepareTax>();
        var taxesEl = firstPt.ValueKind != JsonValueKind.Undefined
            ? firstPt.TryGet("tax_data")?.TryGet("taxes")
            : null;

        if (taxesEl is not null && taxesEl.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in taxesEl.Value.EnumerateArray())
            {
                taxes.Add(new BookingPrepareTax
                {
                    Name         = t.TryGet("name").GetStringSafe()          ?? string.Empty,
                    Amount       = t.TryGet("amount").GetStringSafe()        ?? "0.00",
                    CurrencyCode = t.TryGet("currency_code").GetStringSafe() ?? showCurrency
                });
            }
        }

        // Cancellation policies
        var cancelPolicies  = new List<BookingPrepareCancelPolicy>();
        var freeCancelBefore = string.Empty;

        var penaltiesEl = firstPt.ValueKind != JsonValueKind.Undefined
            ? firstPt.TryGet("cancellation_penalties")
            : null;

        if (penaltiesEl.HasValue)
        {
            freeCancelBefore = penaltiesEl.Value.TryGet("free_cancellation_before").GetStringSafe()
                               ?? string.Empty;

            var policiesEl = penaltiesEl.Value.TryGet("policies");
            if (policiesEl.HasValue && policiesEl.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var pol in policiesEl.Value.EnumerateArray())
                {
                    cancelPolicies.Add(new BookingPrepareCancelPolicy
                    {
                        FromDate = pol.TryGet("start_at").GetStringSafe(),
                        ToDate   = pol.TryGet("end_at").GetStringSafe(),
                        Amount   = pol.TryGet("amount_show").GetDecimalSafe() ?? 0m,
                        Currency = showCurrency
                    });
                }
            }
        }

        var refundable = !string.IsNullOrWhiteSpace(freeCancelBefore)
                         || cancelPolicies.All(p => p.FromDate == null);

        var cancelStatus = refundable ? "Refundable" : "NonRefundable";

        response.Rooms.Add(new BookingPrepareRoomInfo
        {
            RoomType                 = roomName,
            MealType                 = meal,
            TotalPrice               = showAmount,
            Currency                 = showCurrency,
            Refundable               = refundable,
            CancellationPolicyStatus = cancelStatus,
            CancelPolicies           = cancelPolicies,
            DailyPrices              = dailyPrices,
            PaymentType              = paymentType,
            Taxes                    = taxes,
            FreeCancellationBefore   = string.IsNullOrWhiteSpace(freeCancelBefore)
                                       ? null
                                       : freeCancelBefore
        });

        return response;
    }
}
