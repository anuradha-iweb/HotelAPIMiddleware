namespace HotelAPIMiddleware.Infrastructure.Configuration;

public class SearchTuningOptions
{
    public int MaxHotelsPerProvider { get; set; } = 100;

    public int StaticEnrichmentConcurrency { get; set; } = 8;

    public int StaticCacheTtlMinutes { get; set; } = 30;

    public int StaticNegativeCacheTtlMinutes { get; set; } = 5;
}
