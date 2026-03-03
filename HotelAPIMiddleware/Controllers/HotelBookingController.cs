using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

[ApiController]
[Route("api/booking")]
public class HotelBookingController : ControllerBase
{
    private readonly BookingPrepareService _bookingPrepareService;

    public HotelBookingController(BookingPrepareService bookingPrepareService)
    {
        _bookingPrepareService = bookingPrepareService;
    }

    [HttpPost("booking-prep")]
    public async Task<IActionResult> BookingPrepare([FromBody] UnifiedBookingPrepareRequest request, CancellationToken ct)
    {
        if (request.Data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return BadRequest(new { message = "Data is required." });

        if (!TryMapRequest(request, out var mappedRequest, out var error))
            return BadRequest(new { message = error });

        var response = await _bookingPrepareService.PrepareAsync(mappedRequest!, ct);

        if (string.Equals(response.Status, "error", StringComparison.OrdinalIgnoreCase))
            return BadRequest(response);

        return Ok(response);
    }

    private static bool TryMapRequest(
        UnifiedBookingPrepareRequest request,
        out BookingPrepareRequest? mapped,
        out string? error)
    {
        mapped = null;
        error = null;

        if (request.Provider == HotelProvider.Stuba)
        {
            var quoteId = GetString(request.Data, "QuoteId")
                          ?? GetString(request.Data, "RefNo");

            if (string.IsNullOrWhiteSpace(quoteId))
            {
                error = "For Stuba, Data.QuoteId is required.";
                return false;
            }

            mapped = new BookingPrepareRequest
            {
                Provider = HotelProvider.Stuba,
                RefNo = quoteId
            };
            return true;
        }

        if (request.Provider == HotelProvider.RateHawk)
        {
            var refNo = GetString(request.Data, "RefNo")
                        ?? GetString(request.Data, "Hid")
                        ?? GetString(request.Data, "HotelId");

            if (string.IsNullOrWhiteSpace(refNo))
            {
                error = "For RateHawk, Data.RefNo (or Data.Hid) is required.";
                return false;
            }

            var mappedRequest = new BookingPrepareRequest
            {
                Provider = HotelProvider.RateHawk,
                RefNo = refNo,
                Residency = GetString(request.Data, "Residency") ?? "ae",
                Language = GetString(request.Data, "Language") ?? "en",
                Currency = GetString(request.Data, "Currency") ?? "USD",
                BookHash = GetString(request.Data, "BookHash"),
                PriceIncreasePercent = GetInt(request.Data, "PriceIncreasePercent") ?? 20
            };

            var checkIn = GetString(request.Data, "CheckIn");
            if (!string.IsNullOrWhiteSpace(checkIn))
            {
                if (!DateOnly.TryParse(checkIn, out var ci))
                {
                    error = "Data.CheckIn must be a valid date (yyyy-MM-dd).";
                    return false;
                }
                mappedRequest.CheckIn = ci;
            }

            var checkOut = GetString(request.Data, "CheckOut");
            if (!string.IsNullOrWhiteSpace(checkOut))
            {
                if (!DateOnly.TryParse(checkOut, out var co))
                {
                    error = "Data.CheckOut must be a valid date (yyyy-MM-dd).";
                    return false;
                }
                mappedRequest.CheckOut = co;
            }

            if (TryGetPropertyIgnoreCase(request.Data, "Guests", out var guestsEl) &&
                guestsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var guestEl in guestsEl.EnumerateArray())
                {
                    if (guestEl.ValueKind != JsonValueKind.Object)
                        continue;

                    var guest = new BookingGuestRequest
                    {
                        Adults = GetInt(guestEl, "Adults") ?? 1
                    };

                    if (TryGetPropertyIgnoreCase(guestEl, "Children", out var childrenEl) &&
                        childrenEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var child in childrenEl.EnumerateArray())
                        {
                            if (child.ValueKind == JsonValueKind.Number && child.TryGetInt32(out var age))
                                guest.Children.Add(age);
                        }
                    }

                    mappedRequest.Guests.Add(guest);
                }
            }

            mapped = mappedRequest;
            return true;
        }

        error = $"Unsupported provider: {request.Provider}";
        return false;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var prop))
            return null;

        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
            return n;

        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var s))
            return s;

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var prop in element.EnumerateObject())
        {
            if (!string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                continue;

            value = prop.Value;
            return true;
        }

        return false;
    }
}
