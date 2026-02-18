using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Requests;

public class BookingPrepareRequest
{
    /// <summary>
    /// Which provider owns this booking: Stuba or RateHawk.
    /// </summary>
    public HotelProvider Provider { get; set; }

    /// <summary>
    /// For Stuba  → QuoteId (e.g. "1090428833-906") from the search result.
    /// For RateHawk → hid (numeric hotel ID, e.g. "13649126") from the search result.
    /// </summary>
    public string RefNo { get; set; } = string.Empty;

    // ── RateHawk-only fields (used for /search/hp/ call) ──────────────────────

    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public string? Residency { get; set; } = "ae";
    public string? Language { get; set; } = "en";
    public string? Currency { get; set; } = "USD";

    /// <summary>
    /// Guest configuration for RateHawk HP search.
    /// </summary>
    public List<BookingGuestRequest> Guests { get; set; } = new();

    /// <summary>
    /// For RateHawk: the book_hash (h- prefix) of the specific rate the user
    /// selected from the search results. If omitted the first available rate
    /// from the /search/hp/ response will be used.
    /// </summary>
    public string? BookHash { get; set; }

    /// <summary>
    /// For RateHawk /hotel/prebook/: maximum price increase the caller accepts (%).
    /// Default 20 %.
    /// </summary>
    public int PriceIncreasePercent { get; set; } = 20;
}

public class BookingGuestRequest
{
    public int Adults { get; set; } = 1;
    public List<int> Children { get; set; } = new();
}
