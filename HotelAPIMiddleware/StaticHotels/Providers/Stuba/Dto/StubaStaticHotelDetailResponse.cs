using System.Text.Json.Serialization;

namespace HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;

/// <summary>
/// Response from POST {ContentBaseUrl}/getAllHotelsDetailsByHotelIds
/// </summary>
public sealed class StubaGetHotelDetailsResponse
{
    [JsonPropertyName("Success")]
    public bool Success { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Data")]
    public List<StubaHotelDataItem> Data { get; set; } = new();
}

public sealed class StubaHotelDataItem
{
    [JsonPropertyName("HotelElement")]
    public StubaHotelElement? HotelElement { get; set; }
}

public sealed class StubaHotelElement
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Region")]
    public StubaHotelRegion? Region { get; set; }

    [JsonPropertyName("Address")]
    public StubaHotelAddress? Address { get; set; }

    [JsonPropertyName("Stars")]
    public int Stars { get; set; }

    [JsonPropertyName("GeneralInfo")]
    public StubaHotelGeneralInfo? GeneralInfo { get; set; }

    [JsonPropertyName("Photo")]
    public List<StubaHotelPhoto>? Photo { get; set; }

    [JsonPropertyName("Description")]
    public List<StubaHotelDescription>? Description { get; set; }

    [JsonPropertyName("Rating")]
    public StubaHotelRating? Rating { get; set; }
}

public sealed class StubaHotelRegion
{
    [JsonPropertyName("CityId")]
    public int CityId { get; set; }

    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class StubaHotelAddress
{
    [JsonPropertyName("Address1")]
    public string Address1 { get; set; } = string.Empty;

    [JsonPropertyName("Address2")]
    public string Address2 { get; set; } = string.Empty;

    [JsonPropertyName("Address3")]
    public string Address3 { get; set; } = string.Empty;

    [JsonPropertyName("City")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("State")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("Zip")]
    public string Zip { get; set; } = string.Empty;

    [JsonPropertyName("Country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("Tel")]
    public string Tel { get; set; } = string.Empty;
}

public sealed class StubaHotelGeneralInfo
{
    [JsonPropertyName("Latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("Longitude")]
    public double Longitude { get; set; }
}

public sealed class StubaHotelPhoto
{
    [JsonPropertyName("Url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("Width")]
    public int Width { get; set; }

    [JsonPropertyName("Height")]
    public int Height { get; set; }

    [JsonPropertyName("Caption")]
    public string Caption { get; set; } = string.Empty;

    [JsonPropertyName("PhotoType")]
    public string PhotoType { get; set; } = string.Empty;
}

public sealed class StubaHotelDescription
{
    [JsonPropertyName("Language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("Text")]
    public string Text { get; set; } = string.Empty;
}

public sealed class StubaHotelRating
{
    [JsonPropertyName("System")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("Score")]
    public double Score { get; set; }
}
