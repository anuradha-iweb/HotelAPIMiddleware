using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.RateHawk.Dto;
using Microsoft.Extensions.Caching.Memory;

namespace HotelAPIMiddleware.Services;

public class HotelDetailsService
{
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    public HotelDetailsService(IMemoryCache cache, IHttpClientFactory httpClientFactory)
    {
        _cache = cache;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HotelResult?> GetHotelDetailsAsync(GetHotelDetailsRequest request, CancellationToken ct)
    {
        if (!_cache.TryGetValue(request.Data.CacheId, out HotelSearchResponse? cachedResponse) || cachedResponse is null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.Data.SearchId)
            && !string.Equals(cachedResponse.SearchId, request.Data.SearchId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var cacheHotel = cachedResponse.Hotels.FirstOrDefault(h =>
            string.Equals(h.UniqueId, request.Data.HotelId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(h.HotelId, request.Data.HotelId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(h.Id, request.Data.HotelId, StringComparison.OrdinalIgnoreCase));

        if (cacheHotel is null)
            return null;

        var result = CloneHotel(cacheHotel);

        if (request.Provider == HotelProvider.RateHawk)
            await EnrichFromRateHawkHotelPageAsync(result, request.Data, ct);

        MarkSelectedRoom(result, request.Data.RoomId);

        return result;
    }

    private async Task EnrichFromRateHawkHotelPageAsync(HotelResult result, GetHotelDetailsDataRequest request, CancellationToken ct)
    {
        if (request.Hid <= 0 || string.IsNullOrWhiteSpace(request.Checkin) || string.IsNullOrWhiteSpace(request.Checkout))
            return;

        var http = _httpClientFactory.CreateClient("RateHawkClient");

        var hpRequest = new RateHawkHotelPageRequest
        {
            CheckIn = request.Checkin,
            CheckOut = request.Checkout,
            Residency = request.Residency,
            Language = request.Language,
            Currency = request.Currency,
            Timeout = request.Timeout,
            Hid = request.Hid,
            Guests = request.Guests.Count > 0
                ? request.Guests.Select(g => new RateHawkGuest
                {
                    Adults = Math.Max(1, g.Adults),
                    Children = g.Children ?? new List<int>()
                }).ToList()
                : new List<RateHawkGuest> { new() { Adults = 1 } }
        };

        var json = JsonSerializer.Serialize(hpRequest);
        var msg = new HttpRequestMessage(HttpMethod.Post, "search/hp/")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var resp = await http.SendAsync(msg, ct);
        if (!resp.IsSuccessStatusCode)
            return;

        var body = await resp.Content.ReadAsStringAsync(ct);
        RateHawkResponseMapper.MergeHotelPageData(result, body);
    }

    private static void MarkSelectedRoom(HotelResult hotel, string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            return;

        foreach (var room in hotel.Rooms)
            room.IsSelected = string.Equals(room.RoomId, roomId, StringComparison.OrdinalIgnoreCase);
    }

    private static HotelResult CloneHotel(HotelResult hotel)
    {
        var json = JsonSerializer.Serialize(hotel);
        return JsonSerializer.Deserialize<HotelResult>(json) ?? new HotelResult();
    }
}
