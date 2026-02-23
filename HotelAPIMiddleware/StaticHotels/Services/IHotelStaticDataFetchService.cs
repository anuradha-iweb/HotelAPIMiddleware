using HotelAPIMiddleware.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Models;

namespace HotelAPIMiddleware.StaticHotels.Services;

/// <summary>
/// Orchestrates the full pipeline for fetching and storing hotel static data:
///   1. getAllSearchRegionsByCountry → get cities in the region
///   2. RegionSearch (today's date) → discover available hotel IDs per city
///   3. getAllHotelsDetailsByHotelIds → get static detail (images, descriptions, etc.)
///   4. Save each hotel profile as {hotelId}.json
/// </summary>
public interface IHotelStaticDataFetchService
{
    /// <summary>
    /// Runs the full pipeline for a given STUBA search region.
    ///
    /// Steps:
    ///   1. Fetch cities that belong to <paramref name="regionId"/>
    ///   2. For each city's search regions (CityWiseRegionList), run RegionSearch
    ///      using today as arrival date
    ///   3. Collect unique hotel IDs from availability results
    ///   4. Fetch hotel static details in batches
    ///   5. Save each hotel JSON file (create if new, overwrite if changed)
    /// </summary>
    /// <param name="regionId">STUBA search region ID (country or sub-region level).</param>
    /// <param name="nationality">2-letter nationality code for availability search (e.g. "GB").</param>
    /// <param name="nights">Number of nights for availability search.</param>
    /// <param name="rooms">Room configuration for availability search.</param>
    /// <param name="arrivalDate">Arrival date for RegionSearch. Defaults to today when null.</param>
    Task<SyncSummary> FetchByRegionAsync(
        int regionId,
        string nationality,
        int nights,
        IEnumerable<StubaRoom> rooms,
        DateOnly? arrivalDate = null,
        CancellationToken ct = default);
}
