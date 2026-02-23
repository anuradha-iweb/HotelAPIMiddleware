using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

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
    /// Maximum number of concurrent hotel-detail HTTP calls during fetch.
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// Optional millisecond delay between successive API calls.
    /// </summary>
    public int PageDelayMs { get; set; } = 100;

    /// <summary>
    /// Authority credentials for STUBA's static-content API.
    /// These are embedded in every request body (not in headers).
    /// </summary>
    public StubaAuthorityOptions Authority { get; set; } = new();

    /// <summary>
    /// STUBA endpoint paths (relative to StubaContent:BaseUrl in appsettings).
    /// </summary>
    public StubaStaticEndpoints StubaEndpoints { get; set; } = new();
}

/// <summary>
/// Endpoint paths for STUBA's static-content API.
/// </summary>
public class StubaStaticEndpoints
{
    /// <summary>
    /// POST {ContentBaseUrl}/getAllCountries
    /// Returns all country-level RegionId / RegionName values.
    /// </summary>
    public string GetAllCountries { get; set; } = "getAllCountries";

    /// <summary>
    /// POST {ContentBaseUrl}/getAllSearchRegionsByCountry
    /// Returns either search regions or cities depending on the supplied RegionId level.
    /// </summary>
    public string GetSearchRegions { get; set; } = "getAllSearchRegionsByCountry";

    /// <summary>
    /// POST {ContentBaseUrl}/getAllHotelsDetailsByHotelIds
    /// Returns hotel static details including images and descriptions.
    /// </summary>
    public string GetHotelDetails { get; set; } = "getAllHotelsDetailsByHotelIds";
}

/// <summary>
/// Extension to convert the config Authority to the DTO used in request bodies.
/// </summary>
public static class StubaAuthorityOptionsExtensions
{
    public static StubaContentAuthority ToDto(this StubaAuthorityOptions opts) =>
        new()
        {
            Org = opts.Org,
            User = opts.User,
            Password = opts.Password
        };
}
