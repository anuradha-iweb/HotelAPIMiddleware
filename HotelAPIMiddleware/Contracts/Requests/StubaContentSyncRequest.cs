namespace HotelAPIMiddleware.Contracts.Requests;

public class StubaContentSyncRequest
{
    /// <summary>
    /// Region IDs to sync. If omitted or empty, falls back to DefaultRegionIds
    /// configured in appsettings.json under Providers:StubaContent:DefaultRegionIds.
    /// </summary>
    public List<int>? RegionIds { get; set; }

    /// <summary>Whether to request image data for each hotel. Defaults to true.</summary>
    public bool IncludeImages { get; set; } = true;

    /// <summary>Whether to request text description for each hotel. Defaults to true.</summary>
    public bool IncludeDescription { get; set; } = true;
}
