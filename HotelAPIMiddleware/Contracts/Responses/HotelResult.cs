using System.Text.Json;

namespace HotelAPIMiddleware.Contracts.Responses;

public class HotelResult
{
    public string UniqueId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string RefNo { get; set; } = string.Empty;

    /// <summary>Provider hotel id from source search payload (e.g. stuba hotel.id).</summary>
    public string Id { get; set; } = string.Empty;

    public string HotelId { get; set; } = string.Empty;   
    public string Name { get; set; } = string.Empty;
    public int StarRating { get; set; }

    public Address Address { get; set; } = new();

    public List<RoomResult> Rooms { get; set; } = new();

    /// <summary>
    /// External static-content id used to resolve persisted STUBA hotel JSON (hotelAvailability.hotel.id).
    /// </summary>
    public string StaticDataId { get; set; } = string.Empty;

    /// <summary>
    /// Raw static hotel JSON object for STUBA hotels matched by HotelId file name.
    /// For providers without static data support (e.g., RATEHAWK), this remains null.
    /// </summary>
    public JsonElement? StaticData { get; set; }
}
