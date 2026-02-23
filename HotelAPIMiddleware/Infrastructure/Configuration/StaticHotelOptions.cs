namespace HotelAPIMiddleware.Infrastructure.Configuration;

/// <summary>
/// Configuration for the Hotel Static Data feature.
/// Bound from appsettings.json "StaticHotels" section.
/// </summary>
public class StaticHotelOptions
{
    /// <summary>
    /// Root folder where hotel JSON files and index.json are stored.
    /// Relative paths are resolved from the application's content root.
    /// </summary>
    public string StoragePath { get; set; } = "HotelStaticData";

    /// <summary>
    /// Maximum number of concurrent hotel-detail HTTP calls during sync.
    /// Tune this to respect STUBA rate limits (default: 5).
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// Number of hotel IDs to fetch per page when paging the STUBA hotel list.
    /// </summary>
    public int HotelListPageSize { get; set; } = 1000;

    /// <summary>
    /// Optional millisecond delay between successive paged hotel-list calls
    /// to avoid hammering the STUBA API.
    /// </summary>
    public int PageDelayMs { get; set; } = 100;

    /// <summary>
    /// STUBA static-content endpoint names (relative to Stuba BaseUrl).
    /// Update these if STUBA changes their API paths.
    ///
    /// PLACEHOLDER NOTICE: Verify each endpoint name against the official
    /// STUBA Static Content API documentation before going to production.
    /// </summary>
    public StubaStaticEndpoints StubaEndpoints { get; set; } = new();
}

/// <summary>
/// Relative endpoint names for STUBA's static-content API.
/// All paths are appended to the Providers:Stuba:BaseUrl configured in appsettings.
///
/// PLACEHOLDER: Replace these with the real STUBA endpoint names once confirmed.
/// Current placeholders follow common STUBA B2B API naming conventions.
/// </summary>
public class StubaStaticEndpoints
{
    /// <summary>POST {BaseUrl}/GetHotelList — paged list of all hotel IDs.</summary>
    public string HotelList { get; set; } = "GetHotelList";

    /// <summary>POST {BaseUrl}/GetHotelContent — static details for a single hotel.</summary>
    public string HotelContent { get; set; } = "GetHotelContent";

    /// <summary>POST {BaseUrl}/GetDestinationList — list of all regions/destinations.</summary>
    public string RegionList { get; set; } = "GetDestinationList";

    /// <summary>POST {BaseUrl}/GetDestinationHotels — hotel IDs within a region.</summary>
    public string RegionHotels { get; set; } = "GetDestinationHotels";
}
