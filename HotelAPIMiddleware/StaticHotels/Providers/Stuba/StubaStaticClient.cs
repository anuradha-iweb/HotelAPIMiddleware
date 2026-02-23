using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba;

/// <summary>
/// HTTP client for STUBA's static-content API.
/// Uses the shared "StubaClient" named HttpClient (same auth + base URL).
///
/// PLACEHOLDER NOTE: The endpoint names used below are configurable via
/// appsettings.json → StaticHotels:StubaEndpoints. Update them to match
/// the exact paths in the official STUBA Static Content API documentation.
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

    // ── Public interface ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<string>> GetAllHotelIdsAsync(CancellationToken ct = default)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int page = 1;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var req = new StubaStaticHotelListRequest
            {
                PageIndex = page,
                PageSize = _opts.HotelListPageSize
            };

            var resp = await PostAsync<StubaStaticHotelListResponse>(
                _opts.StubaEndpoints.HotelList, req, ct);

            if (resp is null || resp.HotelList.Count == 0)
                break;

            foreach (var item in resp.HotelList)
                if (!string.IsNullOrWhiteSpace(item.HotelId))
                    ids.Add(item.HotelId.Trim());

            _logger.LogInformation(
                "STUBA hotel list page {Page}: fetched {Count} IDs (running total {Total})",
                page, resp.HotelList.Count, ids.Count);

            // Stop if we've collected everything
            if (ids.Count >= resp.TotalRecord || resp.HotelList.Count < _opts.HotelListPageSize)
                break;

            page++;

            if (_opts.PageDelayMs > 0)
                await Task.Delay(_opts.PageDelayMs, ct);
        }

        return ids.ToList();
    }

    public async Task<StubaHotelDetail?> GetHotelDetailAsync(string hotelId, CancellationToken ct = default)
    {
        var req = new StubaStaticHotelDetailRequest { HotelId = hotelId };

        var resp = await PostAsync<StubaStaticHotelDetailResponse>(
            _opts.StubaEndpoints.HotelContent, req, ct);

        return resp?.Hotel;
    }

    public async Task<IReadOnlyList<StubaDestination>> GetAllRegionsAsync(CancellationToken ct = default)
    {
        // PLACEHOLDER: adjust request body if STUBA requires parameters
        var resp = await PostAsync<StubaStaticRegionListResponse>(
            _opts.StubaEndpoints.RegionList, new { }, ct);

        return resp?.Destinations ?? new List<StubaDestination>();
    }

    public async Task<IReadOnlyList<string>> GetHotelIdsByRegionAsync(
        string regionId, CancellationToken ct = default)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int page = 1;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var req = new StubaStaticRegionHotelsRequest
            {
                DestinationId = regionId,
                PageIndex = page,
                PageSize = _opts.HotelListPageSize
            };

            var resp = await PostAsync<StubaStaticRegionHotelsResponse>(
                _opts.StubaEndpoints.RegionHotels, req, ct);

            if (resp is null || resp.HotelList.Count == 0)
                break;

            foreach (var item in resp.HotelList)
                if (!string.IsNullOrWhiteSpace(item.HotelId))
                    ids.Add(item.HotelId.Trim());

            if (ids.Count >= resp.TotalRecord || resp.HotelList.Count < _opts.HotelListPageSize)
                break;

            page++;

            if (_opts.PageDelayMs > 0)
                await Task.Delay(_opts.PageDelayMs, ct);
        }

        return ids.ToList();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<TResponse?> PostAsync<TResponse>(
        string endpoint, object requestBody, CancellationToken ct)
        where TResponse : class
    {
        var http = _httpFactory.CreateClient("StubaClient");
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
            _logger.LogError(ex, "STUBA static client HTTP error calling {Endpoint}", endpoint);
            return null;
        }

        var body = await httpResp.Content.ReadAsStringAsync(ct);

        if (!httpResp.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "STUBA {Endpoint} returned HTTP {Status}: {Body}",
                endpoint, (int)httpResp.StatusCode, body);
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(body, _jsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "STUBA {Endpoint}: failed to deserialize response. Body snippet: {Snippet}",
                endpoint, body.Length > 500 ? body[..500] : body);
            return null;
        }
    }
}
