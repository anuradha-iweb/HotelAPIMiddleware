using HotelAPIMiddleware.StaticHotels.Models;

namespace HotelAPIMiddleware.StaticHotels.Store;

/// <summary>
/// Persistence abstraction for hotel static profiles and the index file.
/// Default implementation: <see cref="HotelStaticFileStore"/> (file system).
/// </summary>
public interface IHotelStaticStore
{
    // ── Hotel profile ─────────────────────────────────────────────────────────

    /// <summary>
    /// Saves a hotel profile atomically (write to .tmp then replace).
    /// Creates the provider/hotels directory if it does not exist.
    /// </summary>
    Task SaveHotelAsync(HotelStaticProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Returns the stored profile for a hotel, or null if not found.
    /// </summary>
    Task<HotelStaticProfile?> GetHotelAsync(string provider, string hotelId, CancellationToken ct = default);

    // ── Index ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the index file for the given provider.
    /// Returns a new empty index if the file does not exist yet.
    /// </summary>
    Task<HotelIndexFile> LoadIndexAsync(string provider, CancellationToken ct = default);

    /// <summary>
    /// Saves the index file atomically.
    /// </summary>
    Task SaveIndexAsync(string provider, HotelIndexFile index, CancellationToken ct = default);

    // ── Listing ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated slice of index entries for the given provider.
    /// </summary>
    Task<HotelPageResult> ListHotelsAsync(
        string provider, int page, int pageSize, CancellationToken ct = default);
}
