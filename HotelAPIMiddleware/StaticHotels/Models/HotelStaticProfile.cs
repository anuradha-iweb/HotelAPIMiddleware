namespace HotelAPIMiddleware.StaticHotels.Models;

/// <summary>
/// Provider-agnostic representation of a hotel's static content.
/// Stored as {hotelId}.json inside the provider subfolder.
/// </summary>
public class HotelStaticProfile
{
    // ── Identity ─────────────────────────────────────────────────────────────
    /// <summary>Provider name, e.g. "STUBA".</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Hotel ID as issued by the provider.</summary>
    public string ProviderHotelId { get; set; } = string.Empty;

    // ── Core ─────────────────────────────────────────────────────────────────
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;

    /// <summary>Numeric star rating (1–5). 0 means unknown.</summary>
    public int StarRating { get; set; }

    /// <summary>e.g. "City Hotel", "Resort", "Apartment"</summary>
    public string Category { get; set; } = string.Empty;

    // ── Location ─────────────────────────────────────────────────────────────
    public HotelAddress Address { get; set; } = new();
    public GeoCoordinates Geo { get; set; } = new();

    // ── Contact ──────────────────────────────────────────────────────────────
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;

    // ── Operational ──────────────────────────────────────────────────────────
    /// <summary>e.g. "14:00"</summary>
    public string CheckInTime { get; set; } = string.Empty;

    /// <summary>e.g. "12:00"</summary>
    public string CheckOutTime { get; set; } = string.Empty;

    public List<string> Languages { get; set; } = new();

    // ── Media ────────────────────────────────────────────────────────────────
    public List<HotelImage> Images { get; set; } = new();

    // ── Facilities ───────────────────────────────────────────────────────────
    public List<string> Facilities { get; set; } = new();

    // ── Audit (excluded from hash computation) ────────────────────────────────
    /// <summary>SHA-256 of the content fields (excludes this field and LastSyncedUtc).</summary>
    public string ContentHash { get; set; } = string.Empty;

    public DateTime LastSyncedUtc { get; set; }
}

public class HotelAddress
{
    public string Line1 { get; set; } = string.Empty;
    public string Line2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class GeoCoordinates
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class HotelImage
{
    public string Url { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}
