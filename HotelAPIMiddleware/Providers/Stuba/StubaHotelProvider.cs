using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.Interfaces;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace HotelAPIMiddleware.Providers.Stuba;

public class StubaHotelProvider : IHotelProvider
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<StubaHotelProvider> _logger;

    public StubaHotelProvider(IHttpClientFactory factory, ILogger<StubaHotelProvider> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public HotelProvider Provider => HotelProvider.Stuba;

    public async Task<ProviderSearchResult> SearchAsync(UnifiedHotelSearchRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var http = _factory.CreateClient("StubaClient"); // ✅ guaranteed Stuba base url

        var stReq = ProviderRequestMappers.ToStubaRequest(request);
        var json = JsonSerializer.Serialize(stReq);

        var msg = new HttpRequestMessage(HttpMethod.Post, "RegionSearch")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var resp = await http.SendAsync(msg, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            sw.Stop();
            _logger.LogWarning(
                "Stuba search failed statusCode={StatusCode} elapsedMs={ElapsedMs}",
                (int)resp.StatusCode,
                sw.ElapsedMilliseconds);

            return new ProviderSearchResult
            {
                Provider = Provider,
                Success = false,
                ErrorMessage = $"Stuba HTTP {(int)resp.StatusCode}: {body}"
            };
        }

        // ✅ Heuristic mapper (works even if Stuba response shape differs)
        var hotels = StubaResponseMapper.MapHotelsFromJson(body, defaultCurrency: request.Currency ?? "USD");
        sw.Stop();
        _logger.LogInformation(
            "Stuba search succeeded hotels={HotelCount} elapsedMs={ElapsedMs}",
            hotels.Count,
            sw.ElapsedMilliseconds);

        foreach (var h in hotels)
        {
            h.Provider = "STUBA";
            foreach (var rm in h.Rooms)
                foreach (var rate in rm.Rates)
                    rate.Provider = "STUBA";
        }

        return new ProviderSearchResult
        {
            Provider = Provider,
            Success = true,
            Hotels = hotels
        };
    }
}
