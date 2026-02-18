using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.RateHawk.Dto;
using HotelAPIMiddleware.Providers.Stuba.Dto;

namespace HotelAPIMiddleware.Services;

/// <summary>
/// Orchestrates the booking-prepare step for both Stuba and RateHawk.
///
/// Stuba  → single call  : POST /BookingPrepare  (uses QuoteId / refNo)
/// RateHawk → two calls  :
///   1. POST /search/hp/      (uses hid / refNo + search params)
///   2. POST /hotel/prebook/  (uses book_hash from step 1 or from request)
/// </summary>
public class BookingPrepareService
{
    private readonly IHttpClientFactory _factory;

    public BookingPrepareService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<BookingPrepareResponse> PrepareAsync(
        BookingPrepareRequest request,
        CancellationToken ct)
    {
        return request.Provider switch
        {
            HotelProvider.Stuba    => await HandleStubaAsync(request, ct),
            HotelProvider.RateHawk => await HandleRateHawkAsync(request, ct),
            _                      => ErrorResponse("UNKNOWN", request.RefNo, $"Unknown provider: {request.Provider}")
        };
    }

    // ── Stuba ─────────────────────────────────────────────────────────────────

    private async Task<BookingPrepareResponse> HandleStubaAsync(
        BookingPrepareRequest request,
        CancellationToken ct)
    {
        var http = _factory.CreateClient("StubaClient");

        var stubaReq = new StubaBookingPrepareRequest { QuoteId = request.RefNo };
        var json     = JsonSerializer.Serialize(stubaReq);

        Console.WriteLine($"[BookingPrepare] Stuba: POST BookingPrepare  QuoteId={request.RefNo}");

        var msg = new HttpRequestMessage(HttpMethod.Post, "BookingPrepare")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var resp = await http.SendAsync(msg, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            return ErrorResponse("STUBA", request.RefNo,
                $"Stuba HTTP {(int)resp.StatusCode}: {body}");
        }

        return StubaBookingPrepareMapper.MapFromJson(body, request.RefNo);
    }

    // ── RateHawk ──────────────────────────────────────────────────────────────

    private async Task<BookingPrepareResponse> HandleRateHawkAsync(
        BookingPrepareRequest request,
        CancellationToken ct)
    {
        if (!long.TryParse(request.RefNo, out var hid))
        {
            return ErrorResponse("RATEHAWK", request.RefNo,
                $"RefNo must be a numeric hotel ID (hid) for RateHawk. Got: '{request.RefNo}'");
        }

        var http = _factory.CreateClient("RateHawkClient");

        // ── Step 1: /search/hp/ – get fresh book_hash values ─────────────────
        var guests = request.Guests.Count > 0
            ? request.Guests
            : new List<BookingGuestRequest> { new() { Adults = 1 } };

        var hpReq = new RateHawkHotelPageRequest
        {
            CheckIn   = request.CheckIn?.ToString("yyyy-MM-dd")   ?? string.Empty,
            CheckOut  = request.CheckOut?.ToString("yyyy-MM-dd")  ?? string.Empty,
            Residency = (request.Residency ?? "ae").ToLowerInvariant(),
            Language  = request.Language  ?? "en",
            Currency  = request.Currency  ?? "USD",
            Hid       = hid,
            Timeout   = 30,
            Guests    = guests.Select(g => new RateHawkGuest
            {
                Adults   = Math.Max(1, g.Adults),
                Children = g.Children ?? new List<int>()
            }).ToList()
        };

        var hpJson = JsonSerializer.Serialize(hpReq);
        Console.WriteLine($"[BookingPrepare] RateHawk Step 1: POST search/hp/  hid={hid}");

        var hpMsg = new HttpRequestMessage(HttpMethod.Post, "search/hp/")
        {
            Content = new StringContent(hpJson, Encoding.UTF8, "application/json")
        };

        var hpResp = await http.SendAsync(hpMsg, ct);
        var hpBody = await hpResp.Content.ReadAsStringAsync(ct);

        if (!hpResp.IsSuccessStatusCode)
        {
            return ErrorResponse("RATEHAWK", request.RefNo,
                $"RateHawk /search/hp/ HTTP {(int)hpResp.StatusCode}: {hpBody}");
        }

        // Pick the book_hash to use for prebook
        var bookHash = RateHawkBookingPrepareMapper.ExtractBookHashFromHp(hpBody, request.BookHash);

        if (string.IsNullOrWhiteSpace(bookHash))
        {
            return ErrorResponse("RATEHAWK", request.RefNo,
                "No book_hash found in /search/hp/ response. The hotel may be unavailable.");
        }

        // ── Step 2: /hotel/prebook/ ───────────────────────────────────────────
        var prebookReq = new RateHawkPrebookRequest
        {
            Hash                 = bookHash,
            PriceIncreasePercent = request.PriceIncreasePercent
        };

        var prebookJson = JsonSerializer.Serialize(prebookReq);
        Console.WriteLine($"[BookingPrepare] RateHawk Step 2: POST hotel/prebook/  hash={bookHash}");

        var pbMsg = new HttpRequestMessage(HttpMethod.Post, "hotel/prebook/")
        {
            Content = new StringContent(prebookJson, Encoding.UTF8, "application/json")
        };

        var pbResp = await http.SendAsync(pbMsg, ct);
        var pbBody = await pbResp.Content.ReadAsStringAsync(ct);

        if (!pbResp.IsSuccessStatusCode)
        {
            return ErrorResponse("RATEHAWK", request.RefNo,
                $"RateHawk /hotel/prebook/ HTTP {(int)pbResp.StatusCode}: {pbBody}");
        }

        var checkIn  = request.CheckIn?.ToString("yyyy-MM-dd")  ?? string.Empty;
        var checkOut = request.CheckOut?.ToString("yyyy-MM-dd") ?? string.Empty;

        return RateHawkBookingPrepareMapper.MapFromPrebookJson(pbBody, request.RefNo, checkIn, checkOut);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BookingPrepareResponse ErrorResponse(string provider, string refNo, string error)
        => new()
        {
            Provider    = provider,
            RefNo       = refNo,
            CommitLevel = string.Empty,
            Status      = "error",
            Error       = error
        };
}
