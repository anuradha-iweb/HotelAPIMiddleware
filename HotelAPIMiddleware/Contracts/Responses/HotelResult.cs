namespace HotelAPIMiddleware.Contracts.Responses;

public class HotelResult
{
    public string UniqueId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string RefNo { get; set; } = string.Empty;
    public string HotelId { get; set; } = string.Empty;   
    public string Name { get; set; } = string.Empty;
    public int StarRating { get; set; }

    public Address Address { get; set; } = new();

    public List<RoomResult> Rooms { get; set; } = new();
}
