using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.Interfaces;
using HotelAPIMiddleware.Providers.RateHawk.Dto;
using System.Text;
using System.Text.Json;

namespace HotelAPIMiddleware.Providers.RateHawk;

public class RateHawkHotelProvider : IHotelProvider
{
    private readonly IHttpClientFactory _factory;

    public RateHawkHotelProvider(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public HotelProvider Provider => HotelProvider.RateHawk;

    public async Task<ProviderSearchResult> SearchAsync(
     UnifiedHotelSearchRequest request,
     CancellationToken ct)
    {
        var http = _factory.CreateClient("RateHawkClient");

        // Step 1: Call /search/serp/region/
        var rhReq = ProviderRequestMappers.ToRateHawkRequest(request);
        var json = JsonSerializer.Serialize(rhReq);

        var msg = new HttpRequestMessage(HttpMethod.Post, "search/serp/region/")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var finalUrl = new Uri(http.BaseAddress!, msg.RequestUri!);
        Console.WriteLine("RateHawk SERP URL = " + finalUrl);

        var resp = await http.SendAsync(msg, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            return new ProviderSearchResult
            {
                Provider = HotelProvider.RateHawk,
                Success = false,
                ErrorMessage = $"RateHawk HTTP {(int)resp.StatusCode}: {body}"
            };
        }

        // Step 2: Map response only (no enrichment)
        var hotels = RateHawkResponseMapper.MapHotelsFromJson(body);

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
        Console.WriteLine($"\n🔄 Starting HP enrichment for {hotels.Count} hotels...\n");

        var tasks = hotels.Select(async hotel =>
        {
            try
            {
                // Parse HID from hotel
                if (!long.TryParse(hotel.RefNo, out var hid))
                {
                    Console.WriteLine($"❌ Hotel {hotel.HotelId} ({hotel.Name}): Could not parse HID from RefNo: {hotel.RefNo}");
                    return;
                }

                Console.WriteLine($"📞 Calling HP for hotel {hotel.HotelId} (HID: {hid})...");

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
                Console.WriteLine($"   Request: {hpJson}");

                var hpMsg = new HttpRequestMessage(HttpMethod.Post, "search/hp/")
                {
                    Content = new StringContent(hpJson, Encoding.UTF8, "application/json")
                };

                var hpResp = await http.SendAsync(hpMsg, ct);
                var hpBody = await hpResp.Content.ReadAsStringAsync(ct);

                if (hpResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ HP Success for hotel {hotel.HotelId}");

                    // Parse HP response and merge data
                    RateHawkResponseMapper.MergeHotelPageData(hotel, hpBody);
                }
                else
                {
                    Console.WriteLine($"❌ HP call failed for hotel {hotel.HotelId}: HTTP {(int)hpResp.StatusCode}");
                    Console.WriteLine($"   Response: {(hpBody.Length > 500 ? hpBody.Substring(0, 500) + "..." : hpBody)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception enriching hotel {hotel.HotelId}: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
            }
        });

        await Task.WhenAll(tasks);

        Console.WriteLine($"\n✅ HP enrichment complete!\n");
    }
}