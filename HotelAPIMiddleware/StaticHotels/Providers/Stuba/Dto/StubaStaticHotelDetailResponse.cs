using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Response from POST {BaseUrl}/GetHotelContent
///
/// PLACEHOLDER: Verify all field names against official STUBA docs.
/// StarRating is sometimes returned as a string (e.g. "4") — adjust parsing in mapper.
/// </summary>
public sealed class StubaStaticHotelDetailResponse
{
    [JsonPropertyName("Hotel")]
    public StubaHotelDetail? Hotel { get; set; }
}

public sealed class StubaHotelDetail
{
    [JsonPropertyName("HotelId")]
    public string HotelId { get; set; } = string.Empty;

    [JsonPropertyName("HotelName")]
    public string HotelName { get; set; } = string.Empty;

    /// <summary>PLACEHOLDER — STUBA may return this as string "4" or int 4.</summary>
    [JsonPropertyName("StarRating")]
    public string StarRating { get; set; } = string.Empty;

    [JsonPropertyName("Category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("ShortDescription")]
    public string ShortDescription { get; set; } = string.Empty;

    [JsonPropertyName("LongDescription")]
    public string LongDescription { get; set; } = string.Empty;

    // ── Address fields ───────────────────────────────────────────────────────
    [JsonPropertyName("Address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("Address2")]
    public string Address2 { get; set; } = string.Empty;

    [JsonPropertyName("City")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("State")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("Country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("CountryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("PostalCode")]
    public string PostalCode { get; set; } = string.Empty;

    // ── Geo ──────────────────────────────────────────────────────────────────
    [JsonPropertyName("Latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("Longitude")]
    public double Longitude { get; set; }

    // ── Contact ──────────────────────────────────────────────────────────────
    [JsonPropertyName("Telephone")]
    public string Telephone { get; set; } = string.Empty;

    [JsonPropertyName("Email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("Website")]
    public string Website { get; set; } = string.Empty;

    // ── Operational ──────────────────────────────────────────────────────────
    [JsonPropertyName("CheckIn")]
    public string CheckIn { get; set; } = string.Empty;

    [JsonPropertyName("CheckOut")]
    public string CheckOut { get; set; } = string.Empty;

    [JsonPropertyName("Languages")]
    public List<string> Languages { get; set; } = new();

    // ── Media / Facilities ────────────────────────────────────────────────────
    [JsonPropertyName("Images")]
    public List<StubaHotelImage> Images { get; set; } = new();

    [JsonPropertyName("Facilities")]
    public List<string> Facilities { get; set; } = new();
}

public sealed class StubaHotelImage
{
    [JsonPropertyName("ImageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("ImageCaption")]
    public string ImageCaption { get; set; } = string.Empty;

    [JsonPropertyName("IsMain")]
    public bool IsMain { get; set; }
}
