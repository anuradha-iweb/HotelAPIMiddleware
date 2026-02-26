using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Requests;

public sealed class GetHotelDetailsRequest
{
    public HotelProvider Provider { get; set; }
    public GetHotelDetailsDataRequest Data { get; set; } = new();
}

public sealed class GetHotelDetailsDataRequest
{
    public string CacheId { get; set; } = string.Empty;
    public string SearchId { get; set; } = string.Empty;
    public string HotelId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;

    // RateHawk-only request fields
    public string Checkin { get; set; } = string.Empty;
    public string Checkout { get; set; } = string.Empty;
    public string Residency { get; set; } = "ae";
    public string Language { get; set; } = "en";
    public List<BookingGuestRequest> Guests { get; set; } = new();
    public int Timeout { get; set; } = 30;
    public long Hid { get; set; }
    public string Currency { get; set; } = "USD";
}
