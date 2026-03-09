using System.Text.Json;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Mappings;

public static class StubaBookingConfirmMapper
{
    public static BookingConfirmResponse MapFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var response = new BookingConfirmResponse
        {
            Provider = "STUBA",
            Currency = root.TryGet("currency").GetStringSafe() ?? "USD",
            CommitLevel = root.TryGet("commitLevel").GetStringSafe() ?? "confirm",
            Status = "ok"
        };

        var bookingEl = root.TryGet("booking");
        if (bookingEl is null)
            return response;

        response.BookingId = bookingEl.Value.TryGet("id").GetStringSafe() ?? string.Empty;
        response.CreationDate = bookingEl.Value.TryGet("creationDate").GetStringSafe() ?? string.Empty;
        response.AgentReference = bookingEl.Value.TryGet("agentReference").GetStringSafe() ?? string.Empty;

        var hotelBookingsEl = bookingEl.Value.TryGet("hotelBookings");
        if (hotelBookingsEl is null || hotelBookingsEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        foreach (var hotelBookingEl in hotelBookingsEl.Value.EnumerateArray())
        {
            var hotelBooking = new BookingConfirmHotelBookingResponse
            {
                Id = hotelBookingEl.TryGet("id").GetIntSafe()?.ToString()
                    ?? hotelBookingEl.TryGet("id").GetStringSafe()
                    ?? string.Empty,
                HotelId = hotelBookingEl.TryGet("hotelId").GetIntSafe()?.ToString()
                    ?? hotelBookingEl.TryGet("hotelId").GetStringSafe()
                    ?? string.Empty,
                HotelName = hotelBookingEl.TryGet("hotelName").GetStringSafe() ?? string.Empty,
                CreationDate = hotelBookingEl.TryGet("creationDate").GetStringSafe() ?? string.Empty,
                ArrivalDate = hotelBookingEl.TryGet("arrivalDate").GetStringSafe() ?? string.Empty,
                Nights = hotelBookingEl.TryGet("nights").GetIntSafe() ?? 0,
                TotalPrice = hotelBookingEl.TryGet("totalPrice").GetDecimalSafe() ?? 0m,
                Status = hotelBookingEl.TryGet("status").GetStringSafe() ?? string.Empty
            };

            var roomsEl = hotelBookingEl.TryGet("rooms");
            if (roomsEl is not null && roomsEl.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var roomEl in roomsEl.Value.EnumerateArray())
                {
                    var room = new BookingConfirmRoomResponse
                    {
                        RoomTypeCode = roomEl.TryGet("roomType")?.TryGet("code").GetStringSafe() ?? string.Empty,
                        RoomTypeText = roomEl.TryGet("roomType")?.TryGet("text").GetStringSafe() ?? string.Empty,
                        MealTypeCode = roomEl.TryGet("mealType")?.TryGet("code").GetStringSafe() ?? string.Empty,
                        MealTypeText = roomEl.TryGet("mealType")?.TryGet("text").GetStringSafe() ?? string.Empty,
                        Status = roomEl.TryGet("status").GetStringSafe() ?? string.Empty,
                        CancellationPolicyStatus = roomEl.TryGet("cancellationPolicyStatus").GetStringSafe()
                    };

                    MapGuests(roomEl.TryGet("adult"), room.Adults);
                    MapGuests(roomEl.TryGet("child"), room.Children);
                    MapMessages(roomEl.TryGet("messages"), room.Messages);
                    MapCanxFees(roomEl.TryGet("canxFees"), room.CanxFees);

                    hotelBooking.Rooms.Add(room);
                }
            }

            response.HotelBookings.Add(hotelBooking);
        }

        return response;
    }

    private static void MapGuests(JsonElement? guestsEl, List<BookingConfirmGuestResponse> target)
    {
        if (guestsEl is null || guestsEl.Value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var guestEl in guestsEl.Value.EnumerateArray())
        {
            target.Add(new BookingConfirmGuestResponse
            {
                Id = guestEl.TryGet("id").GetIntSafe()?.ToString()
                    ?? guestEl.TryGet("id").GetStringSafe()
                    ?? string.Empty,
                Title = guestEl.TryGet("title").GetStringSafe() ?? string.Empty,
                First = guestEl.TryGet("first").GetStringSafe() ?? string.Empty,
                Last = guestEl.TryGet("last").GetStringSafe() ?? string.Empty
            });
        }
    }

    private static void MapMessages(JsonElement? messagesEl, List<BookingConfirmMessageResponse> target)
    {
        if (messagesEl is null || messagesEl.Value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var messageEl in messagesEl.Value.EnumerateArray())
        {
            target.Add(new BookingConfirmMessageResponse
            {
                Type = messageEl.TryGet("type").GetStringSafe() ?? string.Empty,
                Text = messageEl.TryGet("text").GetStringSafe() ?? string.Empty
            });
        }
    }

    private static void MapCanxFees(JsonElement? canxFeesEl, List<BookingConfirmCanxFeeResponse> target)
    {
        if (canxFeesEl is null || canxFeesEl.Value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var feeEl in canxFeesEl.Value.EnumerateArray())
        {
            target.Add(new BookingConfirmCanxFeeResponse
            {
                StartDate = feeEl.TryGet("startDate").GetStringSafe() ?? string.Empty,
                Amount = feeEl.TryGet("amount").GetDecimalSafe() ?? 0m
            });
        }
    }
}
