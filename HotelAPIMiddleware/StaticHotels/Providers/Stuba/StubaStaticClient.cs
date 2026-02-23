using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba;

/// <summary>
/// HTTP client for STUBA's two separate APIs:
///
/// Static-content API  → "StubaContentClient" (testcontent.stuba.com/webapi/staticData/)
///   - getAllSearchRegionsByCountry  → regions / cities
///   - getAllHotelsDetailsByHotelIds → hotel detail with images + descriptions
///   Auth: Authority object embedded in every request body.
///
/// Availability API → "StubaClient" (testapijson.stuba.com/)
///   - RegionSearch → live hotel availability (today's arrival date)
///   Auth: AuthApiKey header (already set on the named client).
/// </summary>
public sealed class StubaStaticClient : IStubaStaticClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly StaticHotelOptions _opts;
    private readonly ILogger<StubaStaticClient> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StubaStaticClient(
        IHttpClientFactory httpFactory,
        IOptions<StaticHotelOptions> opts,
        ILogger<StubaStaticClient> logger)
    {
        _httpFactory = httpFactory;
        _opts = opts.Value;
        _logger = logger;
    }

    // ── Regions ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<StubaSearchRegion>> GetRegionsByCountryAsync(
        int regionId, CancellationToken ct = default)
    {
        var req = BuildContentRequest(regionId);
        var resp = await ContentPostAsync<StubaGetRegionsResponse>(
            _opts.StubaEndpoints.GetSearchRegions, req, ct);

        if (resp is null || !resp.Success)
        {
            _logger.LogWarning(
                "getAllSearchRegionsByCountry (regions) returned no data for regionId={Id}", regionId);
            return Array.Empty<StubaSearchRegion>();
        }

        // Filter out items with zero RegionId — these occur when the API actually returned
        // city-level data (CityId/CityName) which serialised into the RegionId field as 0.
        var valid = resp.Data
            .Where(r => r.RegionId > 0 && !string.IsNullOrWhiteSpace(r.RegionName))
            .ToList();

        _logger.LogInformation(
            "getAllSearchRegionsByCountry: got {Count} sub-regions for regionId={Id}",
            valid.Count, regionId);

        return valid;
    }

    // ── Cities ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<StubaSearchCity>> GetCitiesByRegionAsync(
        int regionId, CancellationToken ct = default)
    {
        var req = BuildContentRequest(regionId);
        var resp = await ContentPostAsync<StubaGetCitiesResponse>(
            _opts.StubaEndpoints.GetSearchRegions, req, ct);

        if (resp is null || !resp.Success)
        {
            _logger.LogWarning(
                "getAllSearchRegionsByCountry (cities) returned no data for regionId={Id}", regionId);
            return Array.Empty<StubaSearchCity>();
        }

        // Filter out items with zero CityId — these occur when the API actually returned
        // region-level data (RegionId/RegionName) which deserialised into CityId as 0.
        var valid = resp.Data
            .Where(c => c.CityId > 0)
            .ToList();

        _logger.LogInformation(
            "getAllSearchRegionsByCountry: got {Count} cities for regionId={Id}",
            valid.Count, regionId);

        return valid;
    }

    // ── Availability search ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> SearchHotelIdsByRegionAsync(
        int regionId,
        string nationality,
        int nights,
        IEnumerable<StubaRoom> rooms,
        CancellationToken ct = default)
    {
        var req = new StubaRegionSearchRequest
        {
            RegionId = regionId,
            Nationality = nationality,
            Nights = nights,
            ArrivalDate = DateTime.Today.ToString("yyyy-MM-dd"),
            Timeout = 30,
            Rooms = rooms.ToList()
        };

        var http = _httpFactory.CreateClient("StubaClient");
        var json = JsonSerializer.Serialize(req);
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "RegionSearch")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage httpResp;
        try
        {
            httpResp = await http.SendAsync(httpReq, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "RegionSearch HTTP error for regionId={Id}", regionId);
            return Array.Empty<string>();
        }

        var body = await httpResp.Content.ReadAsStringAsync(ct);

        if (!httpResp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "RegionSearch returned HTTP {Status} for regionId={Id}: {Body}",
                (int)httpResp.StatusCode, regionId,
                body.Length > 300 ? body[..300] : body);
            return Array.Empty<string>();
        }

        var ids = ParseHotelIdsFromAvailability(body);
        _logger.LogInformation(
            "RegionSearch for regionId={Id}: found {Count} hotels", regionId, ids.Count);

        return ids;
    }

    // ── Hotel details ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<StubaHotelElement>> GetHotelDetailsByIdsAsync(
        IEnumerable<string> hotelIds, CancellationToken ct = default)
    {
        var ids = hotelIds.ToList();
        if (ids.Count == 0)
            return Array.Empty<StubaHotelElement>();

        var req = new StubaGetHotelDetailsRequest
        {
            Authority = _opts.Authority.ToDto(),
            HotelIds = ids,
            Options = new StubaHotelDetailOptions
            {
                IncludeImages = true,
                IncludeDescription = true
            }
        };

        var resp = await ContentPostAsync<StubaGetHotelDetailsResponse>(
            _opts.StubaEndpoints.GetHotelDetails, req, ct);

        if (resp is null || !resp.Success)
        {
            _logger.LogWarning(
                "getAllHotelsDetailsByHotelIds failed for {Count} hotels", ids.Count);
            return Array.Empty<StubaHotelElement>();
        }

        var elements = resp.Data
            .Where(d => d.HotelElement is not null)
            .Select(d => d.HotelElement!)
            .ToList();

        _logger.LogInformation(
            "getAllHotelsDetailsByHotelIds: got details for {Count}/{Requested} hotels",
            elements.Count, ids.Count);

        return elements;
    }

    public async Task<StubaHotelElement?> GetHotelDetailAsync(
        string hotelId, CancellationToken ct = default)
    {
        var results = await GetHotelDetailsByIdsAsync(new[] { hotelId }, ct);
        return results.FirstOrDefault();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private StubaGetSearchRegionsRequest BuildContentRequest(int regionId) => new()
    {
        Authority = _opts.Authority.ToDto(),
        RegionId = regionId
    };

    private async Task<TResponse?> ContentPostAsync<TResponse>(
        string endpoint, object requestBody, CancellationToken ct)
        where TResponse : class
    {
        var http = _httpFactory.CreateClient("StubaContentClient");
        var json = JsonSerializer.Serialize(requestBody);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage httpResp;
        try
        {
            httpResp = await http.SendAsync(httpReq, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "STUBA content API HTTP error calling {Endpoint}", endpoint);
            return null;
        }

        var body = await httpResp.Content.ReadAsStringAsync(ct);

        if (!httpResp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "STUBA content {Endpoint} returned HTTP {Status}: {Body}",
                endpoint, (int)httpResp.StatusCode,
                body.Length > 300 ? body[..300] : body);
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(body, _jsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "STUBA content {Endpoint}: failed to deserialize. Snippet: {Snippet}",
                endpoint, body.Length > 500 ? body[..500] : body);
            return null;
        }
    }

    /// <summary>
    /// Extracts hotel IDs from a RegionSearch JSON response.
    /// Handles the hotelAvailability[].hotel.id structure.
    /// </summary>
    private IReadOnlyList<string> ParseHotelIdsFromAvailability(string body)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("hotelAvailability", out var availability))
                return Array.Empty<string>();

            foreach (var entry in availability.EnumerateArray())
            {
                if (entry.TryGetProperty("hotel", out var hotel) &&
                    hotel.TryGetProperty("id", out var idProp))
                {
                    var id = idProp.ValueKind == JsonValueKind.Number
                        ? idProp.GetInt64().ToString()
                        : idProp.GetString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(id))
                        ids.Add(id);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse hotel IDs from RegionSearch response");
        }

        return ids.ToList();
    }
}
