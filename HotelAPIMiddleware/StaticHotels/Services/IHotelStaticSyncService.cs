using HotelAPIMiddleware.StaticHotels.Models;

namespace HotelAPIMiddleware.StaticHotels.Services;

/// <summary>
/// Orchestrates the discovery, fetch, hash-compare, and persistence of
/// hotel static profiles from STUBA.
/// </summary>
public interface IHotelStaticSyncService
{
    /// <summary>
    /// Full sync: discovers ALL hotel IDs from STUBA (via paging),
    /// then fetches and stores each one that is new or changed.
    /// </summary>
    Task<SyncSummary> SyncAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Region sync: discovers hotel IDs only within the given STUBA
    /// destination/region, then fetches and stores each one that is new or changed.
    /// </summary>
    Task<SyncSummary> SyncByRegionAsync(string regionId, CancellationToken ct = default);

    /// <summary>
    /// Single hotel sync: force-refresh the stored profile for one hotel,
    /// regardless of whether its hash has changed.
    /// </summary>
    Task<SyncSummary> SyncHotelAsync(string hotelId, CancellationToken ct = default);
}
