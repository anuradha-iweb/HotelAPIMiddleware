namespace HotelAPIMiddleware.StaticHotels.Models;

/// <summary>
/// Root document of index.json — one file per provider under StoragePath.
/// Example location: HotelStaticData/stuba/index.json
/// </summary>
public class HotelIndexFile
{
    /// <summary>UTC timestamp of the last successfully completed full sync.</summary>
    public DateTime LastFullSyncUtc { get; set; }

    /// <summary>Total number of hotels tracked in this index.</summary>
    public int TotalHotels => Hotels.Count;

    /// <summary>
    /// Dictionary keyed by ProviderHotelId.
    /// Each value holds hash + file path metadata for that hotel.
    /// </summary>
    public Dictionary<string, HotelIndexEntry> Hotels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
