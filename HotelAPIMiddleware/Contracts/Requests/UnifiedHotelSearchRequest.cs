using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Requests;

public class UnifiedHotelSearchRequest
{
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    // Same RegionId used for both in your examples (18870)
    public Dictionary<string, int> RegionId { get; set; } = new();

    // RateHawk specific needed inputs (you said “related data”)
    public string? Residency { get; set; } = "ae";
    public string? Language { get; set; } = "en";
    public string? Currency { get; set; } = "USD";

    // Stuba specific needed inputs
    public string? Nationality { get; set; } = "GB";
    public int TimeoutSeconds { get; set; } = 30;

    // Rooms
    public List<RoomRequest> Rooms { get; set; } = new();

    // ✅ Provider selection (single or multiple)
    public List<HotelProvider> Providers { get; set; } = new();
}
