using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.Interfaces;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.Providers.Stuba;

public class StubaHotelProvider : IHotelProvider
{
    private readonly IHttpClientFactory _factory;

    public StubaHotelProvider(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public HotelProvider Provider => HotelProvider.Stuba;

    public async Task<ProviderSearchResult> SearchAsync(UnifiedHotelSearchRequest request, CancellationToken ct)
    {
        var http = _factory.CreateClient("StubaClient"); // ✅ guaranteed Stuba base url

        var stReq = ProviderRequestMappers.ToStubaRequest(request);
        var json = JsonSerializer.Serialize(stReq);

        var msg = new HttpRequestMessage(HttpMethod.Post, "RegionSearch")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var finalUrl = new Uri(http.BaseAddress!, msg.RequestUri!);
        Console.WriteLine("Stuba URL = " + finalUrl);

        var resp = await http.SendAsync(msg, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            return new ProviderSearchResult
            {
                Provider = Provider,
                Success = false,
                ErrorMessage = $"Stuba HTTP {(int)resp.StatusCode}: {body}"
            };
        }

        // ✅ Heuristic mapper (works even if Stuba response shape differs)
        var hotels = StubaResponseMapper.MapHotelsFromJson(body, defaultCurrency: request.Currency ?? "USD");

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
