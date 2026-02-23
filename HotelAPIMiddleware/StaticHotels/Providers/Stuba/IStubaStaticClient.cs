using HotelAPIMiddleware.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba;

/// <summary>
/// Abstraction over STUBA's static-content HTTP API (testcontent.stuba.com).
/// Auth is embedded in each request body as an Authority object.
/// </summary>
public interface IStubaStaticClient
{
    /// <summary>
    /// Calls getAllSearchRegionsByCountry and returns top-level search regions
    /// (items with RegionId / RegionName).
    /// Pass the country-level RegionId to get its sub-regions.
    /// </summary>
    Task<IReadOnlyList<StubaSearchRegion>> GetRegionsByCountryAsync(
        int regionId, CancellationToken ct = default);

    /// <summary>
    /// Calls getAllSearchRegionsByCountry and returns cities within a region
    /// (items with CityId / CityName / CityWiseRegionList).
    /// </summary>
    Task<IReadOnlyList<StubaSearchCity>> GetCitiesByRegionAsync(
        int regionId, CancellationToken ct = default);

    /// <summary>
    /// Calls the availability RegionSearch endpoint (testapijson.stuba.com/RegionSearch)
    /// and returns the hotel IDs found for the given search region.
    /// ArrivalDate defaults to today.
    /// </summary>
    Task<IReadOnlyList<string>> SearchHotelIdsByRegionAsync(
        int regionId,
        string nationality,
        int nights,
        IEnumerable<StubaRoom> rooms,
        DateOnly? arrivalDate = null,
        CancellationToken ct = default);

    /// <summary>
    /// Calls getAllHotelsDetailsByHotelIds and returns static profiles (with images +
    /// descriptions) for the given hotel IDs.
    /// </summary>
    Task<IReadOnlyList<StubaHotelElement>> GetHotelDetailsByIdsAsync(
        IEnumerable<string> hotelIds, CancellationToken ct = default);

    /// <summary>
    /// Convenience wrapper: fetches static detail for a single hotel.
    /// Returns null if not found or the API returns an error.
    /// </summary>
    Task<StubaHotelElement?> GetHotelDetailAsync(
        string hotelId, CancellationToken ct = default);
}
