using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.Interfaces;
using HotelAPIMiddleware.Providers.RateHawk.Dto;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace HotelAPIMiddleware.Providers.RateHawk;

public class RateHawkHotelProvider : IHotelProvider
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<RateHawkHotelProvider> _logger;

    public RateHawkHotelProvider(IHttpClientFactory factory, ILogger<RateHawkHotelProvider> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public HotelProvider Provider => HotelProvider.RateHawk;

    public async Task<ProviderSearchResult> SearchAsync(
     UnifiedHotelSearchRequest request,
     CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var http = _factory.CreateClient("RateHawkClient");

        // Step 1: Call /search/serp/region/
        var rhReq = ProviderRequestMappers.ToRateHawkRequest(request);
        var json = JsonSerializer.Serialize(rhReq);

        var msg = new HttpRequestMessage(HttpMethod.Post, "search/serp/region/")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var resp = await http.SendAsync(msg, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            sw.Stop();
            _logger.LogWarning(
                "RateHawk search failed statusCode={StatusCode} elapsedMs={ElapsedMs}",
                (int)resp.StatusCode,
                sw.ElapsedMilliseconds);

            return new ProviderSearchResult
            {
                Provider = HotelProvider.RateHawk,
                Success = false,
                ErrorMessage = $"RateHawk HTTP {(int)resp.StatusCode}: {body}"
            };
        }

        // Step 2: Map response only (no enrichment)
        var hotels = RateHawkResponseMapper.MapHotelsFromJson(body);
        sw.Stop();
        _logger.LogInformation(
            "RateHawk search succeeded hotels={HotelCount} elapsedMs={ElapsedMs}",
            hotels.Count,
            sw.ElapsedMilliseconds);

        return new ProviderSearchResult
        {
            Provider = HotelProvider.RateHawk,
            Success = true,
            Hotels = hotels
        };
    }


    private async Task EnrichHotelsWithBookHashAsync(
        List<HotelResult> hotels,
        UnifiedHotelSearchRequest request,
        HttpClient http,
        CancellationToken ct)
    {
        var tasks = hotels.Select(async hotel =>
        {
            try
            {
                // Parse HID from hotel
                if (!long.TryParse(hotel.RefNo, out var hid))
                {
                    return;
                }

                // Build HP request
                var room = request.Rooms.FirstOrDefault() ?? new RoomRequest { Adults = 1 };
                var hpReq = new RateHawkHotelPageRequest
                {
                    CheckIn = request.CheckIn.ToString("yyyy-MM-dd"),
                    CheckOut = request.CheckOut.ToString("yyyy-MM-dd"),
                    Residency = (request.Residency ?? "ae").ToLowerInvariant(),
                    Language = request.Language ?? "en",
                    Currency = request.Currency ?? "USD",
                    Hid = hid,
                    Timeout = 30,
                    Guests = new()
                    {
                        new RateHawkGuest
                        {
                            Adults = Math.Max(1, room.Adults),
                            Children = room.ChildrenAges ?? new()
                        }
                    }
                };

                var hpJson = JsonSerializer.Serialize(hpReq);

                var hpMsg = new HttpRequestMessage(HttpMethod.Post, "search/hp/")
                {
                    Content = new StringContent(hpJson, Encoding.UTF8, "application/json")
                };

                var hpResp = await http.SendAsync(hpMsg, ct);
                var hpBody = await hpResp.Content.ReadAsStringAsync(ct);

                if (hpResp.IsSuccessStatusCode)
                {
                    // Parse HP response and merge data
                    RateHawkResponseMapper.MergeHotelPageData(hotel, hpBody);
                }
            }
            catch
            {
            }
        });

        await Task.WhenAll(tasks);
    }
}
