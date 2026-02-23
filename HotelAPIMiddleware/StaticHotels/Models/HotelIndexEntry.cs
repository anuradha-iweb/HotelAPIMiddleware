namespace HotelAPIMiddleware.StaticHotels.Models;

/// <summary>
/// Lightweight metadata entry stored in index.json for each known hotel.
/// Used for fast hash comparison without loading the full profile file.
/// </summary>
public class HotelIndexEntry
{
    public string ProviderHotelId { get; set; } = string.Empty;
    public string Provider { get; set; } = "STUBA";
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Relative file path within StoragePath, e.g. "stuba/hotels/12345.json".
    /// </summary>
    public string RelativeFilePath { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 of the content fields of the stored HotelStaticProfile.
    /// Compared against a freshly-fetched profile to detect changes.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    public DateTime LastUpdatedUtc { get; set; }
}
