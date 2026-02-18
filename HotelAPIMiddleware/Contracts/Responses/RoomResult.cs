namespace HotelAPIMiddleware.Contracts.Responses;

public class RoomResult
{
    public string RoomId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }

    public List<RateResult> Rates { get; set; } = new();
}
