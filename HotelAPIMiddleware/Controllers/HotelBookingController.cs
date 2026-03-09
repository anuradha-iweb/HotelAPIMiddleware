using System.Text.Json;
using System.Text.RegularExpressions;
using HotelAPIMiddleware.Common;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

[ApiController]
[Route("api/booking")]
public class HotelBookingController : ControllerBase
{
    private readonly BookingPrepareService _bookingPrepareService;
    private readonly BookingConfirmService _bookingConfirmService;
    private readonly HotelDetailsService _hotelDetailsService;

    public HotelBookingController(
        BookingPrepareService bookingPrepareService,
        BookingConfirmService bookingConfirmService,
        HotelDetailsService hotelDetailsService)
    {
        _bookingPrepareService = bookingPrepareService;
        _bookingConfirmService = bookingConfirmService;
        _hotelDetailsService = hotelDetailsService;
    }

    [HttpPost("booking-prep")]
    public async Task<IActionResult> BookingPrepare([FromBody] UnifiedBookingPrepareRequest request, CancellationToken ct)
    {
        if (request.Data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return BadRequest(new { message = "Data is required." });

        if (!TryMapRequest(request, out var mappedRequest, out var error))
            return BadRequest(new { message = error });

        var response = await _bookingPrepareService.PrepareAsync(mappedRequest!, ct);

        if (TryBuildHotelDetailsRequest(request, out var detailsRequest))
        {
            var hotelDetails = await _hotelDetailsService.GetHotelDetailsAsync(detailsRequest!, ct);
            if (hotelDetails is not null)
            {
                response.BasicHotelDetails = new()
                {
                    UniqueId = hotelDetails.UniqueId,
                    HotelId = hotelDetails.HotelId,
                    ProviderHotelId = hotelDetails.Id,
                    Name = hotelDetails.Name,
                    StarRating = hotelDetails.StarRating,
                    Address = hotelDetails.Address
                };

                response.BookingSummary = BuildStaticSummaryFromStaticData(hotelDetails.StaticData);
            }
        }

        if (string.Equals(response.Status, "error", StringComparison.OrdinalIgnoreCase))
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("booking-confirm")]
    public async Task<IActionResult> BookingConfirm([FromBody] UnifiedBookingConfirmRequest request, CancellationToken ct)
    {
        if (request.Data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return BadRequest(ApiResponse<BookingConfirmResponse>.Fail(
                "Validation failed.",
                new[] { "Data is required." }));
        }

        if (!TryMapBookingConfirmRequest(request, out var mappedRequest, out var error))
        {
            return BadRequest(ApiResponse<BookingConfirmResponse>.Fail(
                "Validation failed.",
                new[] { error ?? "Invalid booking-confirm payload." }));
        }

        var response = await _bookingConfirmService.ConfirmAsync(mappedRequest!, ct);
        if (string.Equals(response.Status, "error", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<BookingConfirmResponse>.Fail(
                "Booking confirm failed.",
                new[] { response.Error ?? "Unknown provider error." }));
        }

        return Ok(ApiResponse<BookingConfirmResponse>.Ok(response, "Booking confirm successful."));
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
                          ?? GetString(request.Data, "quoteId")
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

    private static bool TryBuildHotelDetailsRequest(
        UnifiedBookingPrepareRequest request,
        out GetHotelDetailsRequest? detailsRequest)
    {
        detailsRequest = null;

        var cacheId = GetString(request.Data, "CacheId");
        var searchId = GetString(request.Data, "SearchId") ?? string.Empty;
        var hotelId = GetString(request.Data, "HotelId");
        var roomId = GetString(request.Data, "RoomId") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cacheId) || string.IsNullOrWhiteSpace(hotelId))
            return false;

        var data = new GetHotelDetailsDataRequest
        {
            CacheId = cacheId,
            SearchId = searchId,
            HotelId = hotelId,
            RoomId = roomId
        };

        if (request.Provider == HotelProvider.RateHawk)
        {
            data.Checkin = GetString(request.Data, "CheckIn") ?? string.Empty;
            data.Checkout = GetString(request.Data, "CheckOut") ?? string.Empty;
            data.Residency = GetString(request.Data, "Residency") ?? "ae";
            data.Language = GetString(request.Data, "Language") ?? "en";
            data.Currency = GetString(request.Data, "Currency") ?? "USD";
            data.Timeout = GetInt(request.Data, "Timeout") ?? 30;

            var hidRaw = GetString(request.Data, "Hid") ?? GetString(request.Data, "RefNo");
            if (!string.IsNullOrWhiteSpace(hidRaw) && long.TryParse(hidRaw, out var hid))
                data.Hid = hid;

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

                    data.Guests.Add(guest);
                }
            }
        }

        detailsRequest = new GetHotelDetailsRequest
        {
            Provider = request.Provider,
            Data = data
        };

        return true;
    }

    private static bool TryMapBookingConfirmRequest(
        UnifiedBookingConfirmRequest request,
        out BookingConfirmRequest? mapped,
        out string? error)
    {
        mapped = null;
        error = null;

        if (request.Provider != HotelProvider.Stuba)
        {
            error = $"Booking confirm is currently supported only for STUBA. Received: {request.Provider}.";
            return false;
        }

        var quoteId = GetString(request.Data, "QuoteId") ?? GetString(request.Data, "quoteId");
        if (string.IsNullOrWhiteSpace(quoteId))
        {
            error = "For STUBA, Data.QuoteId is required.";
            return false;
        }

        var agentReference = GetString(request.Data, "AgentReference") ?? GetString(request.Data, "agentReference");
        if (string.IsNullOrWhiteSpace(agentReference))
        {
            error = "For STUBA, Data.AgentReference is required.";
            return false;
        }

        if (!TryGetPropertyIgnoreCase(request.Data, "Room", out var roomsEl) &&
            !TryGetPropertyIgnoreCase(request.Data, "Rooms", out roomsEl))
        {
            error = "For STUBA, Data.Room is required and must be an array.";
            return false;
        }

        if (roomsEl.ValueKind != JsonValueKind.Array || roomsEl.GetArrayLength() == 0)
        {
            error = "For STUBA, Data.Room must contain at least one room.";
            return false;
        }

        var mappedRequest = new BookingConfirmRequest
        {
            Provider = HotelProvider.Stuba,
            QuoteId = quoteId,
            AgentReference = agentReference
        };

        foreach (var roomEl in roomsEl.EnumerateArray())
        {
            if (roomEl.ValueKind != JsonValueKind.Object)
            {
                error = "Each Data.Room item must be an object.";
                return false;
            }

            var room = new BookingConfirmRoomRequest();
            if (TryGetPropertyIgnoreCase(roomEl, "Adult", out var adultsEl))
            {
                if (adultsEl.ValueKind != JsonValueKind.Array)
                {
                    error = "Each Data.Room[].Adult must be an array.";
                    return false;
                }

                foreach (var adultEl in adultsEl.EnumerateArray())
                {
                    if (!TryParseGuest(adultEl, out var adult, out error))
                        return false;

                    room.Adults.Add(adult!);
                }
            }

            if (room.Adults.Count == 0)
            {
                error = "Each Data.Room must contain at least one adult.";
                return false;
            }

            if (TryGetPropertyIgnoreCase(roomEl, "Child", out var childrenEl))
            {
                if (childrenEl.ValueKind != JsonValueKind.Array)
                {
                    error = "Each Data.Room[].Child must be an array.";
                    return false;
                }

                foreach (var childEl in childrenEl.EnumerateArray())
                {
                    if (!TryParseGuest(childEl, out var child, out error))
                        return false;

                    room.Children.Add(child!);
                }
            }

            mappedRequest.Rooms.Add(room);
        }

        mapped = mappedRequest;
        return true;
    }

    private static bool TryParseGuest(
        JsonElement guestEl,
        out BookingConfirmGuestRequest? guest,
        out string? error)
    {
        guest = null;
        error = null;

        if (guestEl.ValueKind != JsonValueKind.Object)
        {
            error = "Guest item must be an object with title/first/last.";
            return false;
        }

        var title = GetString(guestEl, "title");
        var first = GetString(guestEl, "first");
        var last = GetString(guestEl, "last");

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
        {
            error = "Guest must include non-empty title, first, and last fields.";
            return false;
        }

        guest = new BookingConfirmGuestRequest
        {
            Title = title,
            First = first,
            Last = last
        };

        return true;
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

    private static BookingPrepareStaticSummary? BuildStaticSummaryFromStaticData(JsonElement? staticData)
    {
        if (!staticData.HasValue || staticData.Value.ValueKind != JsonValueKind.Object)
            return null;

        var root = staticData.Value;

        var descriptionsByType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (TryGetPropertyIgnoreCase(root, "descriptions", out var descriptionsEl) &&
            descriptionsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var d in descriptionsEl.EnumerateArray())
            {
                if (d.ValueKind != JsonValueKind.Object)
                    continue;

                var type = GetString(d, "type");
                var text = GetString(d, "text");
                if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(text))
                    continue;

                if (!descriptionsByType.ContainsKey(type))
                    descriptionsByType[type] = StripHtml(text);
            }
        }

        var overview = FirstNonEmpty(
            descriptionsByType.TryGetValue("PropertyInformation", out var propertyInfo) ? propertyInfo : null,
            descriptionsByType.TryGetValue("SurroundingArea", out var surroundingArea) ? surroundingArea : null,
            descriptionsByType.TryGetValue("RoomTypes", out var roomTypes) ? roomTypes : null);

        var areaActivities = descriptionsByType.TryGetValue("AreaActivities", out var area) ? area : string.Empty;
        var policies = descriptionsByType.TryGetValue("PoliciesDisclaimers", out var policy) ? policy : string.Empty;
        var dining = descriptionsByType.TryGetValue("DiningFacilities", out var diningFacilities) ? diningFacilities : string.Empty;

        var summary = new BookingPrepareStaticSummary
        {
            Overview = overview ?? string.Empty,
            AreaActivities = areaActivities,
            Policies = policies,
            DiningFacilities = dining,
            CheckInTime = GetString(root, "checkInTime") ?? string.Empty,
            CheckOutTime = GetString(root, "checkOutTime") ?? string.Empty,
            Phone = GetString(root, "phone") ?? string.Empty,
            Email = GetString(root, "email") ?? string.Empty,
            Website = GetString(root, "website") ?? string.Empty
        };

        if (TryGetPropertyIgnoreCase(root, "geo", out var geoEl) && geoEl.ValueKind == JsonValueKind.Object)
        {
            summary.Latitude = GetDecimal(geoEl, "latitude") ?? 0m;
            summary.Longitude = GetDecimal(geoEl, "longitude") ?? 0m;
        }

        if (TryGetPropertyIgnoreCase(root, "facilities", out var facilitiesEl) &&
            facilitiesEl.ValueKind == JsonValueKind.Array)
        {
            summary.Facilities = facilitiesEl.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Take(20)
                .Cast<string>()
                .ToList();
        }

        if (TryGetPropertyIgnoreCase(root, "images", out var imagesEl) &&
            imagesEl.ValueKind == JsonValueKind.Array)
        {
            summary.Images = imagesEl.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Object)
                .Select(x => new BookingPrepareImage
                {
                    Url = GetString(x, "url") ?? string.Empty,
                    Caption = GetString(x, "caption") ?? string.Empty,
                    PhotoType = GetString(x, "photoType") ?? string.Empty
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                .Take(8)
                .ToList();
        }

        return summary;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(element, propertyName, out var prop))
            return null;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var d))
            return d;

        if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var s))
            return s;

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string StripHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var withoutTags = Regex.Replace(text, "<.*?>", " ");
        return Regex.Replace(withoutTags, "\\s+", " ").Trim();
    }
}
