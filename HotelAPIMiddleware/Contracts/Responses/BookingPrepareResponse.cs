namespace HotelAPIMiddleware.Contracts.Responses;

/// <summary>
/// Unified booking-prepare response returned for both Stuba and RateHawk.
///
/// Every field is always present. Fields that don't exist in a provider's
/// API are set to their empty / null / false / 0 default so the caller
/// always receives a consistent schema regardless of provider.
/// </summary>
public class BookingPrepareResponse
{
    /// <summary>"STUBA" or "RATEHAWK"</summary>
    public string Provider { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    /// <summary>"prepare" (Stuba) | "prebook" (RateHawk)</summary>
    public string CommitLevel { get; set; } = string.Empty;

    /// <summary>Echo of the RefNo from the request (QuoteId / hid).</summary>
    public string RefNo { get; set; } = string.Empty;

    public BookingPrepareHotelInfo Hotel { get; set; } = new();

    public List<BookingPrepareRoomInfo> Rooms { get; set; } = new();

    // ── RateHawk only (null / false for Stuba) ───────────────────────────────

    /// <summary>
    /// p- prefixed hash returned by /hotel/prebook/.
    /// Pass this in the actual booking step. Null for Stuba.
    /// </summary>
    public string? BookHash { get; set; }

    /// <summary>True when the prebook price differs from the search price. False for Stuba.</summary>
    public bool PriceChanged { get; set; }

    // ── Stuba only (null for RateHawk) ───────────────────────────────────────

    /// <summary>booking.creationDate from the Stuba response. Null for RateHawk.</summary>
    public string? BookingCreationDate { get; set; }

    // ── Status ────────────────────────────────────────────────────────────────
    public string Status { get; set; } = "ok";
    public string? Error { get; set; }
}

public class BookingPrepareHotelInfo
{
    // ── Common ────────────────────────────────────────────────────────────────
    public string HotelId { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string CheckIn { get; set; } = string.Empty;
    public string CheckOut { get; set; } = string.Empty;
    public int Nights { get; set; }

    // ── Stuba only (null for RateHawk) ───────────────────────────────────────

    /// <summary>Numeric booking ID: hotelBookings[].id. Null for RateHawk.</summary>
    public string? BookingId { get; set; }

    /// <summary>hotelBookings[].creationDate. Null for RateHawk.</summary>
    public string? CreationDate { get; set; }

    /// <summary>hotelBookings[].status (e.g. "quoted"). Null for RateHawk.</summary>
    public string? BookingStatus { get; set; }

    /// <summary>hotelBookings[].quoteRefreshStatus (e.g. "ok"). Null for RateHawk.</summary>
    public string? QuoteRefreshStatus { get; set; }
}

public class BookingPrepareRoomInfo
{
    // ── Common ────────────────────────────────────────────────────────────────
    public string RoomType { get; set; } = string.Empty;   // text description
    public string MealType { get; set; } = string.Empty;   // text description
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool Refundable { get; set; }
    public string CancellationPolicyStatus { get; set; } = string.Empty;
    public List<BookingPrepareCancelPolicy> CancelPolicies { get; set; } = new();

    // ── Stuba only (empty / null for RateHawk) ────────────────────────────────

    /// <summary>roomType.code. Empty for RateHawk.</summary>
    public string RoomTypeCode { get; set; } = string.Empty;

    /// <summary>mealType.code. Empty for RateHawk.</summary>
    public string MealTypeCode { get; set; } = string.Empty;

    /// <summary>rooms[].status (e.g. "quoted"). Empty for RateHawk.</summary>
    public string RoomStatus { get; set; } = string.Empty;

    /// <summary>Informational messages from Stuba (General / Internal Note…). Empty list for RateHawk.</summary>
    public List<BookingPrepareMessage> Messages { get; set; } = new();

    // ── RateHawk only (empty / null / false / 0 for Stuba) ────────────────────

    /// <summary>Meal slug (e.g. "nomeal", "breakfast"). Empty for Stuba.</summary>
    public string Meal { get; set; } = string.Empty;

    /// <summary>Structured meal data. Null for Stuba.</summary>
    public MealData? MealData { get; set; }

    /// <summary>Per-night prices as strings. Empty list for Stuba.</summary>
    public List<string> DailyPrices { get; set; } = new();

    /// <summary>Payment type name (e.g. "deposit"). Empty for Stuba.</summary>
    public string PaymentType { get; set; } = string.Empty;

    /// <summary>Charge amount in charge currency (not display). Empty for Stuba.</summary>
    public string ChargeAmount { get; set; } = string.Empty;

    /// <summary>Charge currency code. Empty for Stuba.</summary>
    public string ChargeCurrency { get; set; } = string.Empty;

    public bool IsNeedCreditCardData { get; set; }
    public bool IsNeedCvc { get; set; }

    /// <summary>Payment deadline (null = pay now). Null for Stuba.</summary>
    public string? By { get; set; }

    /// <summary>VAT breakdown. Null for Stuba.</summary>
    public VatData? VatData { get; set; }

    /// <summary>Commission info. Null for Stuba.</summary>
    public CommissionInfo? CommissionInfo { get; set; }

    /// <summary>Tax line-items. Empty list for Stuba.</summary>
    public List<BookingPrepareTax> Taxes { get; set; } = new();

    /// <summary>Free-cancel deadline (ISO datetime string). Null for Stuba.</summary>
    public string? FreeCancellationBefore { get; set; }

    /// <summary>match_hash (stable across refreshes). Empty for Stuba.</summary>
    public string MatchHash { get; set; } = string.Empty;

    /// <summary>Extra room name info. Null for Stuba.</summary>
    public string? RoomNameInfo { get; set; }

    /// <summary>SERP filter tags. Empty list for Stuba.</summary>
    public List<string> SerpFilters { get; set; } = new();

    /// <summary>Available allotment count. 0 for Stuba.</summary>
    public int Allotment { get; set; }

    /// <summary>Amenity tags (e.g. ["non-smoking"]). Empty list for Stuba.</summary>
    public List<string> AmenitiesData { get; set; } = new();

    /// <summary>True if the rate is available for any residency. False for Stuba.</summary>
    public bool AnyResidency { get; set; }

    /// <summary>Deposit details. Null for Stuba.</summary>
    public string? Deposit { get; set; }

    /// <summary>No-show policy details. Null for Stuba.</summary>
    public string? NoShow { get; set; }

    /// <summary>Translated room-type breakdown. Null for Stuba.</summary>
    public RoomDataTrans? RoomDataTrans { get; set; }

    /// <summary>Legal information text. Null for Stuba.</summary>
    public string? LegalInfo { get; set; }

    /// <summary>True if this is a package rate. False for Stuba.</summary>
    public bool IsPackage { get; set; }

    /// <summary>Room characteristic codes. Null for Stuba.</summary>
    public RgExt? RgExt { get; set; }

    /// <summary>Recommended price. Null for Stuba.</summary>
    public string? RecommendedPrice { get; set; }
}

public class BookingPrepareCancelPolicy
{
    /// <summary>Penalty start date. Null = applies from now.</summary>
    public string? FromDate { get; set; }

    /// <summary>Penalty end date. Null for Stuba (Stuba has no end date).</summary>
    public string? ToDate { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class BookingPrepareMessage
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class BookingPrepareTax
{
    public string Name { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}
