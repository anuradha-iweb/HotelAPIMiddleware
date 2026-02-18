using System.Text.Json;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Mappings;

/// <summary>
/// Maps the Stuba /BookingPrepare response JSON to the unified
/// <see cref="BookingPrepareResponse"/>.
///
/// Stuba response shape:
/// {
///   "currency": "EUR",
///   "commitLevel": "prepare",
///   "booking": {
///     "creationDate": "...",
///     "hotelBookings": [{
///       "id": 663177191,
///       "hotelId": 1695273,
///       "hotelName": "...",
///       "arrivalDate": "2026-02-20",
///       "nights": 1,
///       "totalPrice": 216.14,
///       "status": "quoted",
///       "rooms": [{
///         "roomType":   { "code": "7356223", "text": "Executive Suite" },
///         "mealType":   { "code": "1",       "text": "Breakfast" },
///         "messages":   [{ "type": "General", "text": "..." }],
///         "status":     "quoted",
///         "canxFees":   [{ "amount": 216.14}, { "startDate": "...", "amount": 216.14 }],
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
            Provider    = "STUBA",
            Currency    = currency,
            CommitLevel = commitLevel,
            RefNo       = quoteId,
            Status      = "ok"
        };

        var bookingEl = root.TryGet("booking");
        if (bookingEl is null)
            return response;

        var hotelBookingsEl = bookingEl.Value.TryGet("hotelBookings");
        if (hotelBookingsEl is null || hotelBookingsEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        // Use the first hotel booking entry
        var firstBooking = hotelBookingsEl.Value.EnumerateArray().FirstOrDefault();
        if (firstBooking.ValueKind == JsonValueKind.Undefined)
            return response;

        var hotelId    = firstBooking.TryGet("hotelId").GetIntSafe()?.ToString()
                         ?? firstBooking.TryGet("hotelId").GetStringSafe()
                         ?? string.Empty;
        var hotelName  = firstBooking.TryGet("hotelName").GetStringSafe() ?? string.Empty;
        var arrivalDate = firstBooking.TryGet("arrivalDate").GetStringSafe() ?? string.Empty;
        var nights      = firstBooking.TryGet("nights").GetIntSafe() ?? 0;
        var totalPrice  = firstBooking.TryGet("totalPrice").GetDecimalSafe() ?? 0m;

        // Derive checkout from arrival + nights
        var checkOut = string.Empty;
        if (DateOnly.TryParse(arrivalDate, out var arrival))
            checkOut = arrival.AddDays(nights).ToString("yyyy-MM-dd");

        response.Hotel = new BookingPrepareHotelInfo
        {
            HotelId   = hotelId,
            HotelName = hotelName,
            CheckIn   = arrivalDate,
            CheckOut  = checkOut,
            Nights    = nights
        };

        var roomsEl = firstBooking.TryGet("rooms");
        if (roomsEl is null || roomsEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        foreach (var roomEl in roomsEl.Value.EnumerateArray())
        {
            var roomType     = roomEl.TryGet("roomType")?.TryGet("text").GetStringSafe() ?? string.Empty;
            var mealType     = roomEl.TryGet("mealType")?.TryGet("text").GetStringSafe() ?? string.Empty;
            var cancelStatus = roomEl.TryGet("cancellationPolicyStatus").GetStringSafe() ?? string.Empty;
            var refundable   = !string.Equals(cancelStatus, "NonRefundable", StringComparison.OrdinalIgnoreCase);

            var room = new BookingPrepareRoomInfo
            {
                RoomType                  = roomType,
                MealType                  = mealType,
                TotalPrice                = totalPrice,
                Currency                  = currency,
                Refundable                = refundable,
                CancellationPolicyStatus  = cancelStatus
            };

            // Cancellation fees
            var canxFeesEl = roomEl.TryGet("canxFees");
            if (canxFeesEl is not null && canxFeesEl.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var fee in canxFeesEl.Value.EnumerateArray())
                {
                    var amount   = fee.TryGet("amount").GetDecimalSafe() ?? 0m;
                    var fromDate = fee.TryGet("startDate").GetStringSafe();

                    room.CancelPolicies.Add(new BookingPrepareCancelPolicy
                    {
                        FromDate = fromDate,
                        Amount   = amount,
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
                        Text = msg.TryGet("text").GetStringSafe()  ?? string.Empty
                    });
                }
            }

            response.Rooms.Add(room);
        }

        return response;
    }
}
