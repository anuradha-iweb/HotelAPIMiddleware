namespace HotelAPIMiddleware.StaticHotels.Models;

/// <summary>
/// Result of a sync operation (full, region, or single-hotel).
/// Returned in the API response body.
/// </summary>
public class SyncSummary
{
    public string Provider { get; set; } = "STUBA";
    public SyncScope Scope { get; set; } = SyncScope.Full;

    /// <summary>Region ID if scope is Region, hotel ID if scope is Single.</summary>
    public string? ScopeId { get; set; }

    public int TotalDiscovered { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }

    public List<string> FailedHotelIds { get; set; } = new();
    public List<string> ApiStepErrors { get; set; } = new();

    public DateTime StartedUtc { get; set; }
    public DateTime CompletedUtc { get; set; }

    /// <summary>Wall-clock duration of the sync, in seconds.</summary>
    public double DurationSeconds => (CompletedUtc - StartedUtc).TotalSeconds;
}

public enum SyncScope
{
    Full,
    Region,
    Single
}
