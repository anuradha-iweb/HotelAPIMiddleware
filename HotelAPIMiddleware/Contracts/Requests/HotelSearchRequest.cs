using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Requests;

public class HotelSearchRequest
{
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    public string CountryCode { get; set; } = string.Empty;

    public List<RoomRequest> Rooms { get; set; } = new();

    // IMPORTANT: provider selector
    public List<HotelProvider> Providers { get; set; } = new();
}

public class RoomRequest
{
    public int Adults { get; set; }
    public List<int> ChildrenAges { get; set; } = new();
}
