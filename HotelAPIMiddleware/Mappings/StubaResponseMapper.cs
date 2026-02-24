using System.Text.Json;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Mappings;

public static class StubaResponseMapper
{
    public static List<HotelResult> MapHotelsFromJson(string stubaJson, string defaultCurrency)
    {
        using var doc = JsonDocument.Parse(stubaJson);
        var root = doc.RootElement;

        // ✅ Your JSON uses: { currency, hotelAvailability: [...] }
        var currency = root.TryGet("currency").GetStringSafe() ?? defaultCurrency;

        var hotelAvailabilityEl = root.TryGet("hotelAvailability");
        if (hotelAvailabilityEl is null || hotelAvailabilityEl.Value.ValueKind != JsonValueKind.Array)
            return new List<HotelResult>();

        var results = new List<HotelResult>();

        foreach (var ha in hotelAvailabilityEl.Value.EnumerateArray())
        {
            string refNo = "";
            var resEl = ha.TryGet("results");
            var firstResult = resEl.Value.EnumerateArray().FirstOrDefault();
            if (firstResult.ValueKind != JsonValueKind.Undefined)
            {
                refNo = firstResult.TryGet("quoteId").GetStringSafe() ?? "";
            }

            var hotelEl = ha.TryGet("hotel");
            if (hotelEl is null) continue;

            var hotelIdEl = hotelEl.Value.TryGet("id");
            var hotelId = hotelIdEl.GetIntSafe()?.ToString()
                          ?? hotelIdEl.GetStringSafe()
                          ?? Guid.NewGuid().ToString();
            var hotelName = hotelEl.Value.TryGet("name").GetStringSafe() ?? "Unknown Hotel";

            var hotel = new HotelResult
            {
                Provider = "STUBA",
                RefNo = refNo,
                Id = hotelId,
                HotelId = hotelId,
                Name = hotelName,
                StarRating = 0,
                Address = new Address()
            };

            // results[] (each result has quoteId and rooms[])

            if (resEl is null || resEl.Value.ValueKind != JsonValueKind.Array)
            {
                results.Add(hotel);
                continue;
            }

            // Group rooms by roomType.text so UI gets Rooms[] with Rates[]
            var roomMap = new Dictionary<string, RoomResult>(StringComparer.OrdinalIgnoreCase);

            foreach (var res in resEl.Value.EnumerateArray())
            {
                var quoteId = res.TryGet("quoteId").GetStringSafe() ?? "";

                var roomsEl = res.TryGet("rooms");
                if (roomsEl is null || roomsEl.Value.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var r in roomsEl.Value.EnumerateArray())
                {
                    var roomTypeText = r.TryGet("roomType")?.TryGet("text").GetStringSafe() ?? "Room";
                    var roomTypeCode = r.TryGet("roomType")?.TryGet("code").GetStringSafe() ?? "";
                    var mealText = r.TryGet("mealType")?.TryGet("text").GetStringSafe() ?? "";
                    var mealCode = r.TryGet("mealType")?.TryGet("code").GetStringSafe() ?? "";

                    // price is a NUMBER in your JSON (e.g., 81.09)
                    var total = r.TryGet("price").GetDecimalSafe().GetValueOrDefault();

                    var cancelStatus = r.TryGet("cancellationPolicyStatus").GetStringSafe() ?? "";
                    var refundable = !string.Equals(cancelStatus, "NonRefundable", StringComparison.OrdinalIgnoreCase);
                    var cancelPolicyFrom = r.TryGet("cancelPolicyFrom").GetStringSafe() ?? "";

                    // create/find RoomResult
                    if (!roomMap.TryGetValue(roomTypeText, out var room))
                    {
                        room = new RoomResult
                        {
                            RoomId = string.IsNullOrWhiteSpace(roomTypeCode)
                                ? Guid.NewGuid().ToString("N")
                                : roomTypeCode,
                            Name = roomTypeText
                        };
                        roomMap[roomTypeText] = room;
                    }

                    var rateId = string.Join(":", new[]
                    {
                        quoteId,
                        roomTypeCode,
                        mealCode
                    }.Where(s => !string.IsNullOrWhiteSpace(s)));

                    if (string.IsNullOrWhiteSpace(rateId))
                        rateId = Guid.NewGuid().ToString("N");

                    // Create comprehensive RateResult with ALL Stuba fields
                    var rate = new RateResult
                    {
                        Provider = "STUBA",
                        RateId = rateId,
                        TotalAmount = total,
                        Currency = currency,
                        Refundable = refundable,
                        Board = mealText,
                        PaymentType = "",

                        // Stuba specific fields
                        QuoteId = quoteId,
                        RoomTypeCode = roomTypeCode,
                        RoomTypeText = roomTypeText,
                        MealTypeCode = mealCode,
                        MealTypeText = mealText,
                        CancellationPolicyStatus = cancelStatus,
                        CancelPolicyFrom = cancelPolicyFrom,

                        // RateHawk fields - all empty for Stuba
                        MatchHash = "",
                        SearchHash = "",
                        BookHash = "",
                        DailyPrices = new List<string>(),
                        Meal = "",
                        MealData = null,
                        PaymentOptions = null,
                        RgExt = null,
                        RoomNameInfo = "",
                        SerpFilters = new List<string>(),
                        Allotment = 0,
                        AmenitiesData = new List<string>(),
                        AnyResidency = false,
                        RoomDataTrans = null,
                        LegalInfo = "",
                        IsPackage = false
                    };

                    room.Rates.Add(rate);
                }
            }

            hotel.Rooms = roomMap.Values.ToList();
            results.Add(hotel);
        }

        return results;
    }
}