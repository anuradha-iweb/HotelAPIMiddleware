using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HotelAPIMiddleware.StaticHotels.Mappings;

/// <summary>
/// Maps a STUBA getAllHotelsDetailsByHotelIds response element to the
/// provider-agnostic <see cref="HotelStaticProfile"/> model and computes its
/// content hash.
/// </summary>
public static class StubaStaticHotelMapper
{
    private static readonly JsonSerializerOptions _stableOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maps a <see cref="StubaHotelElement"/> to a <see cref="HotelStaticProfile"/>.
    /// ContentHash and LastSyncedUtc are NOT set here — the caller sets them
    /// after comparing the hash against the stored index entry.
    /// </summary>
    public static HotelStaticProfile Map(StubaHotelElement element)
    {
        return new HotelStaticProfile
        {
            Provider = "STUBA",
            ProviderHotelId = element.Id.ToString(),
            Name = element.Name,
            StarRating = element.Stars,

            Descriptions = (element.Description ?? new())
                .Select(d => new HotelDescriptionEntry
                {
                    Language = d.Language,
                    Type = d.Type,
                    Text = d.Text
                })
                .ToList(),

            Address = new HotelAddress
            {
                Line1 = element.Address?.Address1 ?? string.Empty,
                Line2 = string.Join(", ",
                    new[] { element.Address?.Address2, element.Address?.Address3 }
                    .Where(s => !string.IsNullOrWhiteSpace(s))),
                City = element.Address?.City ?? string.Empty,
                State = element.Address?.State ?? string.Empty,
                Country = element.Address?.Country ?? string.Empty,
                PostalCode = element.Address?.Zip ?? string.Empty
            },

            Geo = new GeoCoordinates
            {
                Latitude = element.GeneralInfo?.Latitude ?? 0,
                Longitude = element.GeneralInfo?.Longitude ?? 0
            },

            Phone = element.Address?.Tel ?? string.Empty,

            Images = (element.Photo ?? new())
                .Select(p => new HotelImage
                {
                    Url = p.Url,
                    Caption = p.Caption,
                    PhotoType = p.PhotoType,
                    Width = p.Width,
                    Height = p.Height
                })
                .ToList()
        };
    }

    /// <summary>
    /// Computes the SHA-256 hash of the hotel's stable content fields.
    /// Audit fields (ContentHash, LastSyncedUtc) are intentionally excluded
    /// so that a re-sync of unchanged data produces the same hash.
    /// </summary>
    public static string ComputeContentHash(HotelStaticProfile profile)
    {
        var hashSource = new
        {
            profile.Provider,
            profile.ProviderHotelId,
            profile.Name,
            profile.StarRating,
            profile.Address,
            profile.Geo,
            profile.Phone,
            // Sort lists for deterministic ordering
            Descriptions = profile.Descriptions
                .OrderBy(d => d.Language)
                .ThenBy(d => d.Type)
                .Select(d => new { d.Language, d.Type, d.Text })
                .ToList(),
            Images = profile.Images
                .OrderBy(i => i.Url)
                .Select(i => new { i.Url, i.Caption, i.PhotoType })
                .ToList()
        };

        var json = JsonSerializer.Serialize(hashSource, _stableOpts);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
