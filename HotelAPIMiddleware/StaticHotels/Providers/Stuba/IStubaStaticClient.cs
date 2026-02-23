using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba;

/// <summary>
/// Abstraction over STUBA's static-content HTTP API.
/// Concrete implementation: <see cref="StubaStaticClient"/>.
/// </summary>
public interface IStubaStaticClient
{
    /// <summary>
    /// Fetches ALL hotel IDs from STUBA by paging through the hotel-list endpoint.
    /// Deduplication is applied before returning.
    /// </summary>
    Task<IReadOnlyList<string>> GetAllHotelIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetches static detail for a single hotel.
    /// Returns null if the hotel is not found or STUBA returns an error.
    /// </summary>
    Task<StubaHotelDetail?> GetHotelDetailAsync(string hotelId, CancellationToken ct = default);

    /// <summary>
    /// Fetches all destination/region records from STUBA.
    /// </summary>
    Task<IReadOnlyList<StubaDestination>> GetAllRegionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetches hotel IDs within a specific region/destination by paging.
    /// Deduplication is applied before returning.
    /// </summary>
    Task<IReadOnlyList<string>> GetHotelIdsByRegionAsync(string regionId, CancellationToken ct = default);
}
