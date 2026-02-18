namespace HotelAPIMiddleware.Contracts.Responses;

/// <summary>
/// Unified booking-prepare response returned for both Stuba and RateHawk.
/// </summary>
public class BookingPrepareResponse
{
    /// <summary>"STUBA" or "RATEHAWK"</summary>
    public string Provider { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    /// <summary>"prepare" (Stuba) or "prebook" (RateHawk)</summary>
    public string CommitLevel { get; set; } = string.Empty;

    /// <summary>
    /// Echo of the RefNo supplied in the request (QuoteId / hid).
    /// </summary>
    public string RefNo { get; set; } = string.Empty;

    public BookingPrepareHotelInfo Hotel { get; set; } = new();

    public List<BookingPrepareRoomInfo> Rooms { get; set; } = new();

    /// <summary>
    /// RateHawk only: the p- prefixed hash returned by /hotel/prebook/.
    /// Pass this as the booking hash in the actual booking step.
    /// </summary>
    public string? BookHash { get; set; }

    /// <summary>
    /// RateHawk only: true when the prebook price differs from the search price.
    /// </summary>
    public bool PriceChanged { get; set; }

    public string Status { get; set; } = "ok";

    /// <summary>Populated when Status != "ok".</summary>
    public string? Error { get; set; }
}

public class BookingPrepareHotelInfo
{
    public string HotelId { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string CheckIn { get; set; } = string.Empty;
    public string CheckOut { get; set; } = string.Empty;
    public int Nights { get; set; }
}

public class BookingPrepareRoomInfo
{
    public string RoomType { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool Refundable { get; set; }
    public string CancellationPolicyStatus { get; set; } = string.Empty;
    public List<BookingPrepareCancelPolicy> CancelPolicies { get; set; } = new();

    /// <summary>Stuba: informational messages attached to the room.</summary>
    public List<BookingPrepareMessage> Messages { get; set; } = new();

    // ── RateHawk extras ───────────────────────────────────────────────────────
    public List<string> DailyPrices { get; set; } = new();
    public string PaymentType { get; set; } = string.Empty;
    public List<BookingPrepareTax> Taxes { get; set; } = new();
    public string? FreeCancellationBefore { get; set; }
}

public class BookingPrepareCancelPolicy
{
    public string? FromDate { get; set; }
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
