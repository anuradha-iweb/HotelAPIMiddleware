namespace HotelAPIMiddleware.Contracts.Responses;

public class StubaContentSyncResult
{
    /// <summary>UTC timestamp when the sync started.</summary>
    public DateTime SyncStartedAt { get; set; }

    /// <summary>UTC timestamp when the sync completed.</summary>
    public DateTime SyncCompletedAt { get; set; }

    /// <summary>Region IDs that were processed during this sync run.</summary>
    public List<int> RegionIds { get; set; } = new();

    /// <summary>Number of regions processed.</summary>
    public int RegionsProcessed { get; set; }

    /// <summary>Total unique hotel IDs found across all regions.</summary>
    public int TotalHotelsFound { get; set; }

    /// <summary>Total hotel records that were successfully processed (new + updated + unchanged).</summary>
    public int TotalHotelsProcessed { get; set; }

    /// <summary>Hotels that did not previously exist in storage and were saved for the first time.</summary>
    public int NewHotels { get; set; }

    /// <summary>Hotels that already existed but whose content changed and were updated.</summary>
    public int UpdatedHotels { get; set; }

    /// <summary>Hotels that already existed and whose content was identical (no write performed).</summary>
    public int UnchangedHotels { get; set; }

    /// <summary>Non-fatal errors encountered during the sync (per-region or per-batch).</summary>
    public List<string> Errors { get; set; } = new();
}
