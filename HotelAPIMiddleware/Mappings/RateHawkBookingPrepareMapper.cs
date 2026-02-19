using System.Text.Json;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Mappings;

/// <summary>
/// Helpers for the RateHawk booking-prepare flow.
///
/// Flow:
///   Step 1  POST /search/hp/      → rates[] each with book_hash (h- prefix)
///   Step 2  POST /hotel/prebook/  → confirmed rate with book_hash (p- prefix)
///
/// All Stuba-only fields are set to empty / null / false / 0 defaults so the
/// caller always receives a consistent schema regardless of provider.
/// </summary>
public static class RateHawkBookingPrepareMapper
{
    // ── Step-1 helper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Finds the book_hash to forward to /hotel/prebook/ from the /search/hp/ response.
    ///
    /// • If <paramref name="preferredBookHash"/> is supplied the method looks for
    ///   a rate whose book_hash matches exactly; if not found it falls back to the
    ///   provided value (so a still-valid SERP hash can be used directly).
    /// • If <paramref name="preferredBookHash"/> is null/empty the first available
    ///   book_hash in the response is returned.
    /// </summary>
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

                if (!string.IsNullOrWhiteSpace(preferredBookHash))
                {
                    if (string.Equals(bh, preferredBookHash, StringComparison.OrdinalIgnoreCase))
                        return bh;
                }
                else
                {
                    return bh; // no preference – first wins
                }
            }
        }

        // preferred hash not found in HP response → fall back to the provided value
        return preferredBookHash;
    }

    // ── Step-2 mapper ─────────────────────────────────────────────────────────

    /// <summary>
    /// Maps the /hotel/prebook/ JSON response to the unified BookingPrepareResponse.
    /// Every RateHawk field present in the response is captured; every Stuba-only
    /// field is set to its empty/null/false/0 default.
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
            Provider         = "RATEHAWK",
            CommitLevel      = "prebook",
            RefNo            = refNo,
            PriceChanged     = priceChanged,
            Status           = "ok",
            // Stuba-only top-level field
            BookingCreationDate = null
        };

        if (data is null) return response;

        var hotelsEl = data.Value.TryGet("hotels");
        if (hotelsEl is null || hotelsEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        var hotelEl = hotelsEl.Value.EnumerateArray().FirstOrDefault();
        if (hotelEl.ValueKind == JsonValueKind.Undefined) return response;

        // ── Hotel info ────────────────────────────────────────────────────────
        var hotelId   = hotelEl.TryGet("hid").GetIntSafe()?.ToString()
                        ?? hotelEl.TryGet("hid").GetStringSafe()
                        ?? hotelEl.TryGet("id").GetStringSafe()
                        ?? string.Empty;
        var hotelName = hotelEl.TryGet("id").GetStringSafe() ?? string.Empty; // hotel slug

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
            Nights    = nights,
            // Stuba-only hotel fields
            BookingId          = null,
            CreationDate       = null,
            BookingStatus      = null,
            QuoteRefreshStatus = null
        };

        // ── Rate ──────────────────────────────────────────────────────────────
        var ratesEl = hotelEl.TryGet("rates");
        if (ratesEl is null || ratesEl.Value.ValueKind != JsonValueKind.Array)
            return response;

        var rateEl = ratesEl.Value.EnumerateArray().FirstOrDefault();
        if (rateEl.ValueKind == JsonValueKind.Undefined) return response;

        // Rate-level scalars
        response.BookHash = rateEl.TryGet("book_hash").GetStringSafe();

        var matchHash    = rateEl.TryGet("match_hash").GetStringSafe()    ?? string.Empty;
        var roomName     = rateEl.TryGet("room_name").GetStringSafe()     ?? string.Empty;
        var meal         = rateEl.TryGet("meal").GetStringSafe()          ?? string.Empty;
        var roomNameInfo = rateEl.TryGet("room_name_info").GetStringSafe();           // nullable
        var legalInfo    = rateEl.TryGet("legal_info").GetStringSafe();               // nullable
        var deposit      = rateEl.TryGet("deposit").GetStringSafe();                  // nullable
        var noShow       = rateEl.TryGet("no_show").GetStringSafe();                  // nullable
        var allotment    = rateEl.TryGet("allotment").GetIntSafe()        ?? 0;
        var anyResidency = rateEl.TryGet("any_residency")?.ValueKind     == JsonValueKind.True;
        var isPackage    = rateEl.TryGet("is_package")?.ValueKind        == JsonValueKind.True;

        var dailyPrices = rateEl.TryGet("daily_prices").EnumerateArraySafe()
                              .Select(x => x.GetStringSafe() ?? string.Empty)
                              .ToList();

        var serpFilters = rateEl.TryGet("serp_filters").EnumerateArraySafe()
                              .Select(x => x.GetStringSafe() ?? string.Empty)
                              .ToList();

        var amenitiesData = rateEl.TryGet("amenities_data").EnumerateArraySafe()
                                .Select(x => x.GetStringSafe() ?? string.Empty)
                                .ToList();

        // meal_data
        var mealDataEl = rateEl.TryGet("meal_data");
        MealData? mealData = null;
        if (mealDataEl.HasValue && mealDataEl.Value.ValueKind == JsonValueKind.Object)
        {
            mealData = new MealData
            {
                Value        = mealDataEl.Value.TryGet("value").GetStringSafe()     ?? string.Empty,
                HasBreakfast = mealDataEl.Value.TryGet("has_breakfast")?.ValueKind  == JsonValueKind.True,
                NoChildMeal  = mealDataEl.Value.TryGet("no_child_meal")?.ValueKind  == JsonValueKind.True
            };
        }

        // rg_ext
        var rgExtEl = rateEl.TryGet("rg_ext");
        RgExt? rgExt = null;
        if (rgExtEl.HasValue && rgExtEl.Value.ValueKind == JsonValueKind.Object)
        {
            rgExt = new RgExt
            {
                Class    = rgExtEl.Value.TryGet("class").GetIntSafe()    ?? 0,
                Quality  = rgExtEl.Value.TryGet("quality").GetIntSafe()  ?? 0,
                Sex      = rgExtEl.Value.TryGet("sex").GetIntSafe()      ?? 0,
                Bathroom = rgExtEl.Value.TryGet("bathroom").GetIntSafe() ?? 0,
                Bedding  = rgExtEl.Value.TryGet("bedding").GetIntSafe()  ?? 0,
                Family   = rgExtEl.Value.TryGet("family").GetIntSafe()   ?? 0,
                Capacity = rgExtEl.Value.TryGet("capacity").GetIntSafe() ?? 0,
                Club     = rgExtEl.Value.TryGet("club").GetIntSafe()     ?? 0,
                Bedrooms = rgExtEl.Value.TryGet("bedrooms").GetIntSafe() ?? 0,
                Balcony  = rgExtEl.Value.TryGet("balcony").GetIntSafe()  ?? 0,
                View     = rgExtEl.Value.TryGet("view").GetIntSafe()     ?? 0,
                Floor    = rgExtEl.Value.TryGet("floor").GetIntSafe()    ?? 0
            };
        }

        // room_data_trans
        var rdtEl = rateEl.TryGet("room_data_trans");
        RoomDataTrans? roomDataTrans = null;
        if (rdtEl.HasValue && rdtEl.Value.ValueKind == JsonValueKind.Object)
        {
            roomDataTrans = new RoomDataTrans
            {
                MainRoomType = rdtEl.Value.TryGet("main_room_type").GetStringSafe() ?? string.Empty,
                MainName     = rdtEl.Value.TryGet("main_name").GetStringSafe()      ?? string.Empty,
                Bathroom     = rdtEl.Value.TryGet("bathroom").GetStringSafe()       ?? string.Empty,
                BeddingType  = rdtEl.Value.TryGet("bedding_type").GetStringSafe()   ?? string.Empty,
                MiscRoomType = rdtEl.Value.TryGet("misc_room_type").GetStringSafe() ?? string.Empty
            };
        }

        // ── Payment type (first entry) ────────────────────────────────────────
        var paymentTypesEl = rateEl.TryGet("payment_options")?.TryGet("payment_types");
        var firstPt        = paymentTypesEl?.EnumerateArraySafe().FirstOrDefault() ?? default;
        var hasPt          = firstPt.ValueKind != JsonValueKind.Undefined;

        var showAmount       = hasPt ? firstPt.TryGet("show_amount").GetDecimalSafe()        ?? 0m            : 0m;
        var showCurrency     = hasPt ? firstPt.TryGet("show_currency_code").GetStringSafe()  ?? "USD"         : "USD";
        var paymentType      = hasPt ? firstPt.TryGet("type").GetStringSafe()                ?? string.Empty  : string.Empty;
        var chargeAmount     = hasPt ? firstPt.TryGet("amount").GetStringSafe()              ?? string.Empty  : string.Empty;
        var chargeCurrency   = hasPt ? firstPt.TryGet("currency_code").GetStringSafe()       ?? string.Empty  : string.Empty;
        var by               = hasPt ? firstPt.TryGet("by").GetStringSafe()                                   : null;
        var isNeedCcData     = hasPt && firstPt.TryGet("is_need_credit_card_data")?.ValueKind == JsonValueKind.True;
        var isNeedCvc        = hasPt && firstPt.TryGet("is_need_cvc")?.ValueKind              == JsonValueKind.True;
        var recommendedPrice = hasPt ? firstPt.TryGet("recommended_price").GetStringSafe()                    : null;

        response.Currency = showCurrency;

        // vat_data
        var vatDataEl = hasPt ? firstPt.TryGet("vat_data") : null;
        VatData? vatData = null;
        if (vatDataEl.HasValue && vatDataEl.Value.ValueKind == JsonValueKind.Object)
        {
            vatData = new VatData
            {
                Included     = vatDataEl.Value.TryGet("included")?.ValueKind     == JsonValueKind.True,
                Applied      = vatDataEl.Value.TryGet("applied")?.ValueKind      == JsonValueKind.True,
                Amount       = vatDataEl.Value.TryGet("amount").GetStringSafe()       ?? "0.00",
                CurrencyCode = vatDataEl.Value.TryGet("currency_code").GetStringSafe() ?? string.Empty,
                Value        = vatDataEl.Value.TryGet("value").GetStringSafe()        ?? "0.00"
            };
        }

        // commission_info
        var commInfoEl = hasPt ? firstPt.TryGet("commission_info") : null;
        CommissionInfo? commissionInfo = null;
        if (commInfoEl.HasValue && commInfoEl.Value.ValueKind == JsonValueKind.Object)
        {
            commissionInfo = new CommissionInfo
            {
                Show   = ParseCommissionAmount(commInfoEl.Value.TryGet("show")),
                Charge = ParseCommissionAmount(commInfoEl.Value.TryGet("charge"))
            };
        }

        // tax_data → flat list
        var taxes   = new List<BookingPrepareTax>();
        var taxesEl = hasPt ? firstPt.TryGet("tax_data")?.TryGet("taxes") : null;
        if (taxesEl.HasValue && taxesEl.Value.ValueKind == JsonValueKind.Array)
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

        // cancellation_penalties
        var cancelPolicies   = new List<BookingPrepareCancelPolicy>();
        var freeCancelBefore = string.Empty;

        var penaltiesEl = hasPt ? firstPt.TryGet("cancellation_penalties") : null;
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

        var refundable   = !string.IsNullOrWhiteSpace(freeCancelBefore)
                           || cancelPolicies.All(p => p.FromDate == null);
        var cancelStatus = refundable ? "Refundable" : "NonRefundable";

        // ── Build room ────────────────────────────────────────────────────────
        response.Rooms.Add(new BookingPrepareRoomInfo
        {
            // Common
            RoomType                 = roomName,
            MealType                 = meal,
            TotalPrice               = showAmount,
            Currency                 = showCurrency,
            Refundable               = refundable,
            CancellationPolicyStatus = cancelStatus,
            CancelPolicies           = cancelPolicies,

            // Stuba-only → explicit defaults
            RoomTypeCode             = string.Empty,
            MealTypeCode             = string.Empty,
            RoomStatus               = string.Empty,
            Messages                 = new List<BookingPrepareMessage>(),

            // RateHawk-specific
            Meal                     = meal,
            MealData                 = mealData,
            DailyPrices              = dailyPrices,
            PaymentType              = paymentType,
            ChargeAmount             = chargeAmount,
            ChargeCurrency           = chargeCurrency,
            IsNeedCreditCardData     = isNeedCcData,
            IsNeedCvc                = isNeedCvc,
            By                       = by,
            VatData                  = vatData,
            CommissionInfo           = commissionInfo,
            Taxes                    = taxes,
            FreeCancellationBefore   = string.IsNullOrWhiteSpace(freeCancelBefore) ? null : freeCancelBefore,
            MatchHash                = matchHash,
            RoomNameInfo             = roomNameInfo,
            SerpFilters              = serpFilters,
            Allotment                = allotment,
            AmenitiesData            = amenitiesData,
            AnyResidency             = anyResidency,
            Deposit                  = deposit,
            NoShow                   = noShow,
            RoomDataTrans            = roomDataTrans,
            LegalInfo                = legalInfo,
            IsPackage                = isPackage,
            RgExt                    = rgExt,
            RecommendedPrice         = recommendedPrice
        });

        return response;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static CommissionAmount? ParseCommissionAmount(JsonElement? el)
    {
        if (!el.HasValue || el.Value.ValueKind != JsonValueKind.Object) return null;
        return new CommissionAmount
        {
            AmountGross      = el.Value.TryGet("amount_gross").GetStringSafe()      ?? string.Empty,
            AmountNet        = el.Value.TryGet("amount_net").GetStringSafe()        ?? string.Empty,
            AmountCommission = el.Value.TryGet("amount_commission").GetStringSafe() ?? string.Empty
        };
    }
}
