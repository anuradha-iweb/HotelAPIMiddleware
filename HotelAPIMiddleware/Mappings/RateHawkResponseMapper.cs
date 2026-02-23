using System.Text.Json;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Mappings;

public static class RateHawkResponseMapper
{
    public static List<HotelResult> MapHotelsFromJson(string rateHawkJson)
    {
        using var doc = JsonDocument.Parse(rateHawkJson);
        var root = doc.RootElement;

        // data.hotels[]
        var hotels = root.TryGet("data")?.TryGet("hotels");
        if (hotels is null) return new List<HotelResult>();

        var results = new List<HotelResult>();

        foreach (var h in hotels.EnumerateArraySafe())
        {
            var refNo =
                h.TryGet("hid").GetStringSafe()
                ?? h.TryGet("hid").GetIntSafe()?.ToString()
                ?? h.TryGet("id").GetStringSafe()
                ?? "";

            var hotelId =
                h.TryGet("hid").GetStringSafe()
                ?? h.TryGet("id").GetStringSafe()
                ?? h.TryGet("hotel_id").GetStringSafe()
                ?? Guid.NewGuid().ToString();

            var name =
                h.TryGet("name").GetStringSafe()
                ?? h.TryGet("hotel_name").GetStringSafe()
                ?? "Unknown Hotel";

            var star = h.TryGet("star_rating").GetIntSafe()
                       ?? h.TryGet("stars").GetIntSafe()
                       ?? 0;

            var hotel = new HotelResult
            {
                Id = hotelId,
                HotelId = hotelId,
                RefNo = refNo,
                Name = name,
                StarRating = star,
                Address = new Address
                {
                    CountryCode = h.TryGet("country_code").GetStringSafe() ?? ""
                }
            };

            // Map rates[] into a single "Room" group by room_name
            var rates = h.TryGet("rates");
            var roomMap = new Dictionary<string, RoomResult>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rates.EnumerateArraySafe())
            {
                var roomName = r.TryGet("room_name").GetStringSafe() ?? "Room";
                var matchHash = r.TryGet("match_hash").GetStringSafe() ?? Guid.NewGuid().ToString("N");

                // Total from payment_options.payment_types[0].amount OR fallback
                decimal total =
                    r.TryGet("payment_options")?.TryGet("payment_types")?
                        .EnumerateArraySafe()
                        .Select(x => x.TryGet("amount").GetDecimalSafe())
                        .FirstOrDefault(x => x.HasValue) ?? 0m;

                var currency =
                    r.TryGet("payment_options")?.TryGet("payment_types")?
                        .EnumerateArraySafe()
                        .Select(x => x.TryGet("currency_code").GetStringSafe())
                        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))
                    ?? r.TryGet("currency").GetStringSafe()
                    ?? "USD";

                var refundable =
                    r.TryGet("refundable").GetIntSafe() switch
                    {
                        1 => true,
                        0 => false,
                        _ => r.TryGet("is_refundable").GetIntSafe() == 1
                    };

                if (!roomMap.TryGetValue(roomName, out var room))
                {
                    room = new RoomResult
                    {
                        RoomId = Guid.NewGuid().ToString("N"),
                        Name = roomName
                    };
                    roomMap[roomName] = room;
                }

                // Create comprehensive RateResult with ALL RateHawk fields
                var rate = new RateResult
                {
                    RateId = matchHash,
                    Provider = "RATEHAWK",
                    TotalAmount = total,
                    Currency = currency,
                    Refundable = refundable,

                    // RateHawk specific fields
                    MatchHash = r.TryGet("match_hash").GetStringSafe() ?? "",
                    SearchHash = r.TryGet("search_hash").GetStringSafe() ?? "",
                    BookHash = "", // Will be populated from HP call
                    DailyPrices = r.TryGet("daily_prices").EnumerateArraySafe()
                        .Select(x => x.GetStringSafe() ?? "").ToList(),
                    Meal = r.TryGet("meal").GetStringSafe() ?? "",
                    MealData = MapMealData(r.TryGet("meal_data")),
                    PaymentOptions = MapPaymentOptions(r.TryGet("payment_options")),
                    RgExt = MapRgExt(r.TryGet("rg_ext")),
                    RoomNameInfo = r.TryGet("room_name_info").GetStringSafe() ?? "",
                    SerpFilters = r.TryGet("serp_filters").EnumerateArraySafe()
                        .Select(x => x.GetStringSafe() ?? "").ToList(),
                    Allotment = r.TryGet("allotment").GetIntSafe() ?? 0,
                    AmenitiesData = r.TryGet("amenities_data").EnumerateArraySafe()
                        .Select(x => x.GetStringSafe() ?? "").ToList(),
                    AnyResidency = r.TryGet("any_residency").GetIntSafe() == 1,
                    RoomDataTrans = MapRoomDataTrans(r.TryGet("room_data_trans")),
                    LegalInfo = r.TryGet("legal_info").GetStringSafe() ?? "",
                    IsPackage = r.TryGet("is_package").GetIntSafe() == 1,

                    // Stuba fields - all empty for RateHawk
                    QuoteId = "",
                    RoomTypeCode = "",
                    RoomTypeText = "",
                    MealTypeCode = "",
                    MealTypeText = "",
                    CancellationPolicyStatus = "",
                    CancelPolicyFrom = ""
                };

                room.Rates.Add(rate);
            }

            hotel.Rooms = roomMap.Values.ToList();
            results.Add(hotel);
        }

        return results;
    }

    private static MealData? MapMealData(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var value = el.Value;
        return new MealData
        {
            Value = value.TryGet("value").GetStringSafe() ?? "",
            HasBreakfast = value.TryGet("has_breakfast").GetIntSafe() == 1,
            NoChildMeal = value.TryGet("no_child_meal").GetIntSafe() == 1
        };
    }

    private static PaymentOptions? MapPaymentOptions(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var paymentTypes = new List<PaymentType>();
        var value = el.Value;

        foreach (var pt in value.TryGet("payment_types").EnumerateArraySafe())
        {
            paymentTypes.Add(new PaymentType
            {
                Amount = pt.TryGet("amount").GetStringSafe() ?? "",
                ShowAmount = pt.TryGet("show_amount").GetStringSafe() ?? "",
                CurrencyCode = pt.TryGet("currency_code").GetStringSafe() ?? "",
                ShowCurrencyCode = pt.TryGet("show_currency_code").GetStringSafe() ?? "",
                By = pt.TryGet("by").GetStringSafe() ?? "",
                IsNeedCreditCardData = pt.TryGet("is_need_credit_card_data").GetIntSafe() == 1,
                IsNeedCvc = pt.TryGet("is_need_cvc").GetIntSafe() == 1,
                Type = pt.TryGet("type").GetStringSafe() ?? "",
                VatData = MapVatData(pt.TryGet("vat_data")),
                TaxData = MapTaxData(pt.TryGet("tax_data")),
                CommissionInfo = MapCommissionInfo(pt.TryGet("commission_info")),
                CancellationPenalties = MapCancellationPenalties(pt.TryGet("cancellation_penalties")),
                RecommendedPrice = pt.TryGet("recommended_price").GetStringSafe() ?? ""
            });
        }

        return new PaymentOptions { PaymentTypes = paymentTypes };
    }

    private static VatData? MapVatData(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var value = el.Value;
        return new VatData
        {
            Included = value.TryGet("included").GetIntSafe() == 1,
            Applied = value.TryGet("applied").GetIntSafe() == 1,
            Amount = value.TryGet("amount").GetStringSafe() ?? "",
            CurrencyCode = value.TryGet("currency_code").GetStringSafe() ?? "",
            Value = value.TryGet("value").GetStringSafe() ?? ""
        };
    }

    private static TaxData? MapTaxData(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var taxes = new List<Tax>();
        var value = el.Value;

        foreach (var t in value.TryGet("taxes").EnumerateArraySafe())
        {
            taxes.Add(new Tax
            {
                Name = t.TryGet("name").GetStringSafe() ?? "",
                IncludedBySupplier = t.TryGet("included_by_supplier").GetIntSafe() == 1,
                Amount = t.TryGet("amount").GetStringSafe() ?? "",
                CurrencyCode = t.TryGet("currency_code").GetStringSafe() ?? ""
            });
        }

        return new TaxData { Taxes = taxes };
    }

    private static CommissionInfo? MapCommissionInfo(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var value = el.Value;
        return new CommissionInfo
        {
            Show = MapCommissionAmount(value.TryGet("show")),
            Charge = MapCommissionAmount(value.TryGet("charge"))
        };
    }

    private static CommissionAmount? MapCommissionAmount(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var value = el.Value;
        return new CommissionAmount
        {
            AmountGross = value.TryGet("amount_gross").GetStringSafe() ?? "",
            AmountNet = value.TryGet("amount_net").GetStringSafe() ?? "",
            AmountCommission = value.TryGet("amount_commission").GetStringSafe() ?? ""
        };
    }

    private static CancellationPenalties? MapCancellationPenalties(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var policies = new List<CancellationPolicy>();
        var value = el.Value;

        foreach (var p in value.TryGet("policies").EnumerateArraySafe())
        {
            policies.Add(new CancellationPolicy
            {
                StartAt = p.TryGet("start_at").GetStringSafe() ?? "",
                EndAt = p.TryGet("end_at").GetStringSafe() ?? "",
                AmountCharge = p.TryGet("amount_charge").GetStringSafe() ?? "",
                AmountShow = p.TryGet("amount_show").GetStringSafe() ?? "",
                CommissionInfo = MapCommissionInfo(p.TryGet("commission_info"))
            });
        }

        return new CancellationPenalties
        {
            Policies = policies,
            FreeCancellationBefore = value.TryGet("free_cancellation_before").GetStringSafe() ?? ""
        };
    }

    private static RgExt? MapRgExt(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var value = el.Value;
        return new RgExt
        {
            Class = value.TryGet("class").GetIntSafe() ?? 0,
            Quality = value.TryGet("quality").GetIntSafe() ?? 0,
            Sex = value.TryGet("sex").GetIntSafe() ?? 0,
            Bathroom = value.TryGet("bathroom").GetIntSafe() ?? 0,
            Bedding = value.TryGet("bedding").GetIntSafe() ?? 0,
            Family = value.TryGet("family").GetIntSafe() ?? 0,
            Capacity = value.TryGet("capacity").GetIntSafe() ?? 0,
            Club = value.TryGet("club").GetIntSafe() ?? 0,
            Bedrooms = value.TryGet("bedrooms").GetIntSafe() ?? 0,
            Balcony = value.TryGet("balcony").GetIntSafe() ?? 0,
            View = value.TryGet("view").GetIntSafe() ?? 0,
            Floor = value.TryGet("floor").GetIntSafe() ?? 0
        };
    }

    private static RoomDataTrans? MapRoomDataTrans(JsonElement? el)
    {
        if (!el.HasValue) return null;

        var value = el.Value;
        return new RoomDataTrans
        {
            MainRoomType = value.TryGet("main_room_type").GetStringSafe() ?? "",
            MainName = value.TryGet("main_name").GetStringSafe() ?? "",
            Bathroom = value.TryGet("bathroom").GetStringSafe() ?? "",
            BeddingType = value.TryGet("bedding_type").GetStringSafe() ?? "",
            MiscRoomType = value.TryGet("misc_room_type").GetStringSafe() ?? ""
        };
    }

    /// <summary>
    /// Merges data from /search/hp/ (Hotel Page) endpoint into existing hotel
    /// Updates book_hash and any other fields that differ from SERP response
    /// </summary>
    public static void MergeHotelPageData(HotelResult hotel, string hpJsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(hpJsonResponse);
            var root = doc.RootElement;

            Console.WriteLine($"🔍 HP Response structure for hotel {hotel.HotelId}:");
            Console.WriteLine($"   Root keys: {string.Join(", ", GetJsonKeys(root))}");

            // Try multiple possible response structures
            JsonElement? ratesData = null;
            JsonElement? hotelData = null;

            // Structure 1: { data: { rates: [...], hotel: {...} } }
            var data = root.TryGet("data");
            if (data.HasValue)
            {
                var dataValue = data.Value;
                Console.WriteLine($"   Data keys: {string.Join(", ", GetJsonKeys(dataValue))}");

                ratesData = dataValue.TryGet("rates");
                hotelData = dataValue.TryGet("hotel");
            }

            // Structure 2: { rates: [...], hotel: {...} } - direct on root
            if (!ratesData.HasValue)
            {
                ratesData = root.TryGet("rates");
                hotelData = root.TryGet("hotel");
            }

            // Structure 3: { data: { hotel: { rates: [...] } } }
            if (!ratesData.HasValue && hotelData.HasValue)
            {
                var hotelValue = hotelData.Value;
                Console.WriteLine($"   Hotel keys: {string.Join(", ", GetJsonKeys(hotelValue))}");
                ratesData = hotelValue.TryGet("rates");
            }

            if (!ratesData.HasValue)
            {
                Console.WriteLine($"   ❌ Could not find rates array in HP response");
                return;
            }

            Console.WriteLine($"   ✓ Found rates array with {ratesData.Value.GetArrayLength()} rates");

            // Update hotel-level data if present
            if (hotelData.HasValue)
            {
                var hotelValue = hotelData.Value;

                var name = hotelValue.TryGet("name").GetStringSafe();
                if (!string.IsNullOrWhiteSpace(name))
                    hotel.Name = name;

                var starRating = hotelValue.TryGet("star_rating").GetIntSafe()
                                ?? hotelValue.TryGet("stars").GetIntSafe();
                if (starRating.HasValue && starRating.Value > 0)
                    hotel.StarRating = starRating.Value;

                var countryCode = hotelValue.TryGet("country_code").GetStringSafe();
                if (!string.IsNullOrWhiteSpace(countryCode))
                    hotel.Address.CountryCode = countryCode;
            }

            // Merge rates data - match by match_hash and add book_hash
            int matchedCount = 0;
            int bookHashCount = 0;

            foreach (var hpRate in ratesData.Value.EnumerateArray())
            {
                // Log first rate structure
                if (matchedCount == 0)
                {
                    Console.WriteLine($"   First HP rate keys: {string.Join(", ", GetJsonKeys(hpRate))}");
                }

                var matchHash = hpRate.TryGet("match_hash").GetStringSafe();
                var bookHash = hpRate.TryGet("book_hash").GetStringSafe() ?? "";

                if (!string.IsNullOrWhiteSpace(bookHash))
                {
                    bookHashCount++;
                    if (bookHashCount == 1)
                        Console.WriteLine($"   ✓ Found book_hash: {bookHash}");
                }

                if (string.IsNullOrWhiteSpace(matchHash))
                {
                    Console.WriteLine($"   ⚠ Rate without match_hash, skipping");
                    continue;
                }

                // Find matching rate in existing hotel data
                bool foundMatch = false;
                foreach (var room in hotel.Rooms)
                {
                    var existingRate = room.Rates.FirstOrDefault(r => r.MatchHash == matchHash);
                    if (existingRate != null)
                    {
                        foundMatch = true;
                        matchedCount++;

                        // Update book_hash
                        existingRate.BookHash = bookHash;

                        // Update other fields that might be more detailed in HP response
                        var hpTotal = hpRate.TryGet("payment_options")?.TryGet("payment_types")?
                            .EnumerateArraySafe()
                            .Select(x => x.TryGet("amount").GetDecimalSafe())
                            .FirstOrDefault(x => x.HasValue);

                        if (hpTotal.HasValue && hpTotal.Value > 0)
                            existingRate.TotalAmount = hpTotal.Value;

                        var refundable = hpRate.TryGet("refundable").GetIntSafe();
                        if (refundable.HasValue)
                            existingRate.Refundable = refundable == 1;

                        var mealData = MapMealData(hpRate.TryGet("meal_data"));
                        if (mealData != null)
                            existingRate.MealData = mealData;

                        var paymentOptions = MapPaymentOptions(hpRate.TryGet("payment_options"));
                        if (paymentOptions != null)
                            existingRate.PaymentOptions = paymentOptions;

                        var roomDataTrans = MapRoomDataTrans(hpRate.TryGet("room_data_trans"));
                        if (roomDataTrans != null)
                            existingRate.RoomDataTrans = roomDataTrans;

                        var allotment = hpRate.TryGet("allotment").GetIntSafe();
                        if (allotment.HasValue)
                            existingRate.Allotment = allotment.Value;

                        break;
                    }
                }

                if (!foundMatch && matchedCount == 0)
                {
                    Console.WriteLine($"   ⚠ match_hash '{matchHash}' not found in existing rates");
                }
            }

            Console.WriteLine($"   📊 Matched {matchedCount} rates, found {bookHashCount} book_hashes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error in MergeHotelPageData: {ex.Message}");
        }
    }

    private static List<string> GetJsonKeys(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return new List<string>();

        return element.EnumerateObject().Select(p => p.Name).ToList();
    }
}