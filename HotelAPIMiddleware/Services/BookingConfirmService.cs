using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.Stuba.Dto;

namespace HotelAPIMiddleware.Services;

public class BookingConfirmService
{
    private readonly IHttpClientFactory _factory;

    public BookingConfirmService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<BookingConfirmResponse> ConfirmAsync(BookingConfirmRequest request, CancellationToken ct)
    {
        return request.Provider switch
        {
            HotelProvider.Stuba => await HandleStubaAsync(request, ct),
            HotelProvider.RateHawk => ErrorResponse("RATEHAWK", "Booking confirm is not implemented yet for RateHawk."),
            _ => ErrorResponse("UNKNOWN", $"Unknown provider: {request.Provider}")
        };
    }

    private async Task<BookingConfirmResponse> HandleStubaAsync(BookingConfirmRequest request, CancellationToken ct)
    {
        var http = _factory.CreateClient("StubaClient");

        var stubaReq = new StubaBookingConfirmRequest
        {
            QuoteId = request.QuoteId,
            AgentReference = request.AgentReference,
            Room = request.Rooms.Select(r => new StubaBookingConfirmRoom
            {
                Adult = r.Adults.Select(a => new StubaBookingConfirmGuest
                {
                    Title = a.Title,
                    First = a.First,
                    Last = a.Last
                }).ToList(),
                Child = r.Children.Select(c => new StubaBookingConfirmGuest
                {
                    Title = c.Title,
                    First = c.First,
                    Last = c.Last
                }).ToList()
            }).ToList()
        };

        var json = JsonSerializer.Serialize(stubaReq);
        var msg = new HttpRequestMessage(HttpMethod.Post, "BookingConfirm")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var resp = await http.SendAsync(msg, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            return ErrorResponse("STUBA", $"Stuba HTTP {(int)resp.StatusCode}: {body}");
        }

        return StubaBookingConfirmMapper.MapFromJson(body);
    }

    private static BookingConfirmResponse ErrorResponse(string provider, string error)
        => new()
        {
            Provider = provider,
            Status = "error",
            Error = error
        };
}
