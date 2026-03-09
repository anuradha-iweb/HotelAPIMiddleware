namespace HotelAPIMiddleware.Contracts.Responses;

public class BookingConfirmResponse
{
    public string Provider { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string CommitLevel { get; set; } = string.Empty;
    public string BookingId { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public string AgentReference { get; set; } = string.Empty;
    public string Status { get; set; } = "ok";
    public string? Error { get; set; }
    public List<BookingConfirmHotelBookingResponse> HotelBookings { get; set; } = new();
}

public class BookingConfirmHotelBookingResponse
{
    public string Id { get; set; } = string.Empty;
    public string HotelId { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public string ArrivalDate { get; set; } = string.Empty;
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<BookingConfirmRoomResponse> Rooms { get; set; } = new();
}

public class BookingConfirmRoomResponse
{
    public string RoomTypeCode { get; set; } = string.Empty;
    public string RoomTypeText { get; set; } = string.Empty;
    public string MealTypeCode { get; set; } = string.Empty;
    public string MealTypeText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CancellationPolicyStatus { get; set; }
    public List<BookingConfirmGuestResponse> Adults { get; set; } = new();
    public List<BookingConfirmGuestResponse> Children { get; set; } = new();
    public List<BookingConfirmMessageResponse> Messages { get; set; } = new();
    public List<BookingConfirmCanxFeeResponse> CanxFees { get; set; } = new();
}

public class BookingConfirmGuestResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
}

public class BookingConfirmMessageResponse
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class BookingConfirmCanxFeeResponse
{
    public string StartDate { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
