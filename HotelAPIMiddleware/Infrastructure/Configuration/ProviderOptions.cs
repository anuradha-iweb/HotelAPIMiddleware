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

/// <summary>
/// Configuration for STUBA's static-content API (testcontent.stuba.com).
/// Auth is embedded as an Authority object in each request body.
/// </summary>
public class StubaContentOptions
{
    public string BaseUrl { get; set; } = "https://testcontent.stuba.com/webapi/staticData/";
    public StubaAuthorityOptions Authority { get; set; } = new();
}

public class StubaAuthorityOptions
{
    public string Org { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
