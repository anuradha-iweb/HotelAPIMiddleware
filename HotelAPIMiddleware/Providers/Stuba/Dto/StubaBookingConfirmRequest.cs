using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.Stuba.Dto;

public sealed class StubaBookingConfirmRequest
{
    [JsonPropertyName("QuoteId")]
    public string QuoteId { get; set; } = string.Empty;

    [JsonPropertyName("AgentReference")]
    public string AgentReference { get; set; } = string.Empty;

    [JsonPropertyName("Room")]
    public List<StubaBookingConfirmRoom> Room { get; set; } = new();
}

public sealed class StubaBookingConfirmRoom
{
    [JsonPropertyName("Adult")]
    public List<StubaBookingConfirmGuest> Adult { get; set; } = new();

    [JsonPropertyName("Child")]
    public List<StubaBookingConfirmGuest> Child { get; set; } = new();
}

public sealed class StubaBookingConfirmGuest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("first")]
    public string First { get; set; } = string.Empty;

    [JsonPropertyName("last")]
    public string Last { get; set; } = string.Empty;
}
