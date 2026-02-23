using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.Providers.Stuba.StaticContent.Dto;

/// <summary>
/// Request body for POST /webapi/staticData/getAllHotelsListBySearchRegion
/// </summary>
public sealed class StubaHotelsListByRegionRequest
{
    [JsonPropertyName("Authority")]
    public StubaContentAuthority Authority { get; set; } = new();

    [JsonPropertyName("RegionId")]
    public int RegionId { get; set; }
}
