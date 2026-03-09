using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Requests;

public class BookingConfirmRequest
{
    public HotelProvider Provider { get; set; }
    public string QuoteId { get; set; } = string.Empty;
    public string AgentReference { get; set; } = string.Empty;
    public List<BookingConfirmRoomRequest> Rooms { get; set; } = new();
}

public class BookingConfirmRoomRequest
{
    public List<BookingConfirmGuestRequest> Adults { get; set; } = new();
    public List<BookingConfirmGuestRequest> Children { get; set; } = new();
}

public class BookingConfirmGuestRequest
{
    public string Title { get; set; } = string.Empty;
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
}
