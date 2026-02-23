using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Request body for POST {BaseUrl}/GetHotelList
///
/// PLACEHOLDER: Verify the exact field names against the official
/// STUBA Static Content API documentation.
/// Common STUBA B2B field casing is PascalCase.
/// </summary>
public sealed class StubaStaticHotelListRequest
{
    [JsonPropertyName("PageIndex")]
    public int PageIndex { get; set; } = 1;

    [JsonPropertyName("PageSize")]
    public int PageSize { get; set; } = 1000;
}
