namespace HotelAPIMiddleware.Infrastructure.Configuration;

public class ProviderOptions
{
    public RateHawkOptions RateHawk { get; set; } = new();
    public StubaOptions Stuba { get; set; } = new();
    public StubaContentOptions StubaContent { get; set; } = new();
}

public class RateHawkOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string BasicAuth { get; set; } = string.Empty; // "Basic xxx"
}

public class StubaOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthApiKey { get; set; } = string.Empty;
}

public class StubaContentOptions
{
    public string BaseUrl { get; set; } = "https://testcontent.stuba.com/";
    public string Org { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>Region IDs to process when none are supplied in the sync request.</summary>
    public List<int> DefaultRegionIds { get; set; } = new();
    /// <summary>Directory (relative to ContentRoot) where per-hotel JSON files are stored.</summary>
    public string DataDirectory { get; set; } = "Data/StubaContent";
}
