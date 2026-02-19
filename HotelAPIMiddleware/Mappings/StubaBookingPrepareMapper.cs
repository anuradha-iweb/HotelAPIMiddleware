using System.Text.Json;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Mappings;

/// <summary>
/// Maps the Stuba /BookingPrepare response to the unified BookingPrepareResponse.
///
/// All RateHawk-only fields are set to their empty / null / false / 0 defaults
/// so the caller always gets a consistent schema regardless of provider.
///
/// Stuba response shape:
/// {
///   "currency": "EUR",
///   "commitLevel": "prepare",
///   "booking": {
///     "creationDate": "2026-02-18",
///     "hotelBookings": [{
///       "id": 663177191,
///       "hotelId": 1695273,
///       "hotelName": "Landmark Hotel Riqqa",
///       "creationDate": "2026-02-02",
///       "arrivalDate": "2026-02-20",
///       "nights": 1,
///       "totalPrice": 216.14,
///       "status": "quoted",
///       "rooms": [{
///         "roomType":   { "code": "7356223", "text": "Executive Suite + Free Wifi" },
///         "mealType":   { "code": "1",       "text": "Breakfast" },
///         "messages":   [{ "type": "General", "text": "..." }],
///         "status":     "quoted",
///         "canxFees":   [{ "amount": 216.14 }, { "startDate": "2026-02-18", "amount": 216.14 }],
///         "cancellationPolicyStatus": "NonRefundable"
///       }],
///       "quoteRefreshStatus": "ok"
///     }]
///   }
/// }
/// </summary>
public static class StubaBookingPrepareMapper
{
    public static BookingPrepareResponse MapFromJson(string json, string quoteId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var currency    = root.TryGet("currency").GetStringSafe()    ?? "USD";
        var commitLevel = root.TryGet("commitLevel").GetStringSafe() ?? "prepare";

        var response = new BookingPrepareResponse
        {
            Provider            = "STUBA",
            Currency            = currency,
            CommitLevel         = commitLevel,
            RefNo               = quoteId,
            Status              = "ok",
            // RateHawk-only top-level fields → defaults
            BookHash            = null,
            PriceChanged        = false
        };

        var bookingEl = root.TryGet("booking");
        if (bookingEl is null)
            return response;

        // booking.creationDate (Stuba only)
        response.BookingCreationDate = bookingEl.Value.TryGet("creationDate").GetStringSafe();

        var hotelBookingsEl = bookingEl.Value.TryGet("hotelBookings");
        if (hotelBookingsEl is null || hotelBookingsEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        // Use the first hotel booking
        var firstBooking = hotelBookingsEl.Value.EnumerateArray().FirstOrDefault();
        if (firstBooking.ValueKind == JsonValueKind.Undefined)
            return response;

        // ── Hotel info ────────────────────────────────────────────────────────
        var hotelId    = firstBooking.TryGet("hotelId").GetIntSafe()?.ToString()
                         ?? firstBooking.TryGet("hotelId").GetStringSafe()
                         ?? string.Empty;
        var hotelName        = firstBooking.TryGet("hotelName").GetStringSafe()        ?? string.Empty;
        var arrivalDate      = firstBooking.TryGet("arrivalDate").GetStringSafe()      ?? string.Empty;
        var nights           = firstBooking.TryGet("nights").GetIntSafe()              ?? 0;
        var totalPrice       = firstBooking.TryGet("totalPrice").GetDecimalSafe()      ?? 0m;
        var hotelCreDate     = firstBooking.TryGet("creationDate").GetStringSafe();
        var bookingStatus    = firstBooking.TryGet("status").GetStringSafe();
        var quoteRefreshSt   = firstBooking.TryGet("quoteRefreshStatus").GetStringSafe();
        var bookingId        = firstBooking.TryGet("id").GetIntSafe()?.ToString()
                               ?? firstBooking.TryGet("id").GetStringSafe();

        // Derive checkout from arrival + nights
        var checkOut = string.Empty;
        if (DateOnly.TryParse(arrivalDate, out var arrival))
            checkOut = arrival.AddDays(nights).ToString("yyyy-MM-dd");

        response.Hotel = new BookingPrepareHotelInfo
        {
            HotelId            = hotelId,
            HotelName          = hotelName,
            CheckIn            = arrivalDate,
            CheckOut           = checkOut,
            Nights             = nights,
            // Stuba-specific
            BookingId          = bookingId,
            CreationDate       = hotelCreDate,
            BookingStatus      = bookingStatus,
            QuoteRefreshStatus = quoteRefreshSt
            // RateHawk-only hotel fields have null defaults
        };

        // ── Rooms ─────────────────────────────────────────────────────────────
        var roomsEl = firstBooking.TryGet("rooms");
        if (roomsEl is null || roomsEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        foreach (var roomEl in roomsEl.Value.EnumerateArray())
        {
            var roomType     = roomEl.TryGet("roomType")?.TryGet("text").GetStringSafe() ?? string.Empty;
            var roomTypeCode = roomEl.TryGet("roomType")?.TryGet("code").GetStringSafe() ?? string.Empty;
            var mealType     = roomEl.TryGet("mealType")?.TryGet("text").GetStringSafe() ?? string.Empty;
            var mealTypeCode = roomEl.TryGet("mealType")?.TryGet("code").GetStringSafe() ?? string.Empty;
            var roomStatus   = roomEl.TryGet("status").GetStringSafe()                   ?? string.Empty;
            var cancelStatus = roomEl.TryGet("cancellationPolicyStatus").GetStringSafe() ?? string.Empty;
            var refundable   = !string.Equals(cancelStatus, "NonRefundable", StringComparison.OrdinalIgnoreCase);

            var room = new BookingPrepareRoomInfo
            {
                // Common
                RoomType                 = roomType,
                MealType                 = mealType,
                TotalPrice               = totalPrice,
                Currency                 = currency,
                Refundable               = refundable,
                CancellationPolicyStatus = cancelStatus,

                // Stuba-specific
                RoomTypeCode             = roomTypeCode,
                MealTypeCode             = mealTypeCode,
                RoomStatus               = roomStatus,

                // RateHawk-only → explicit defaults (empty / null / false / 0)
                Meal                     = string.Empty,
                MealData                 = null,
                DailyPrices              = new List<string>(),
                PaymentType              = string.Empty,
                ChargeAmount             = string.Empty,
                ChargeCurrency           = string.Empty,
                IsNeedCreditCardData     = false,
                IsNeedCvc                = false,
                By                       = null,
                VatData                  = null,
                CommissionInfo           = null,
                Taxes                    = new List<BookingPrepareTax>(),
                FreeCancellationBefore   = null,
                MatchHash                = string.Empty,
                RoomNameInfo             = null,
                SerpFilters              = new List<string>(),
                Allotment                = 0,
                AmenitiesData            = new List<string>(),
                AnyResidency             = false,
                Deposit                  = null,
                NoShow                   = null,
                RoomDataTrans            = null,
                LegalInfo                = null,
                IsPackage                = false,
                RgExt                    = null,
                RecommendedPrice         = null
            };

            // Cancellation fees (canxFees[])
            var canxFeesEl = roomEl.TryGet("canxFees");
            if (canxFeesEl is not null && canxFeesEl.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var fee in canxFeesEl.Value.EnumerateArray())
                {
                    room.CancelPolicies.Add(new BookingPrepareCancelPolicy
                    {
                        FromDate = fee.TryGet("startDate").GetStringSafe(),
                        ToDate   = null,   // Stuba has no end-date on fees
                        Amount   = fee.TryGet("amount").GetDecimalSafe() ?? 0m,
                        Currency = currency
                    });
                }
            }

            // Informational messages
            var messagesEl = roomEl.TryGet("messages");
            if (messagesEl is not null && messagesEl.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var msg in messagesEl.Value.EnumerateArray())
                {
                    room.Messages.Add(new BookingPrepareMessage
                    {
                        Type = msg.TryGet("type").GetStringSafe() ?? string.Empty,
                        Text = msg.TryGet("text").GetStringSafe() ?? string.Empty
                    });
                }
            }

            response.Rooms.Add(room);
        }

        return response;
    }
}
