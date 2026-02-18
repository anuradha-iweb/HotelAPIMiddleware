namespace HotelAPIMiddleware.Infrastructure.Configuration;

public class ProviderOptions
{
    public RateHawkOptions RateHawk { get; set; } = new();
    public StubaOptions Stuba { get; set; } = new();
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
