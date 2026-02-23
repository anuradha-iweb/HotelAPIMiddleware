using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HotelAPIMiddleware.StaticHotels.Mappings;

/// <summary>
/// Maps a STUBA static hotel detail response to the provider-agnostic
/// <see cref="HotelStaticProfile"/> model, and computes its content hash.
/// </summary>
public static class StubaStaticHotelMapper
{
    private static readonly JsonSerializerOptions _stableOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Maps the STUBA detail DTO to a <see cref="HotelStaticProfile"/>.
    /// ContentHash and LastSyncedUtc are NOT set here — the caller sets them
    /// after comparing the hash against the stored index entry.
    /// </summary>
    public static HotelStaticProfile Map(StubaHotelDetail detail)
    {
        return new HotelStaticProfile
        {
            Provider = "STUBA",
            ProviderHotelId = detail.HotelId,
            Name = detail.HotelName,
            ShortDescription = detail.ShortDescription,
            LongDescription = detail.LongDescription,
            StarRating = ParseStarRating(detail.StarRating),
            Category = detail.Category,

            Address = new HotelAddress
            {
                Line1 = detail.Address,
                Line2 = detail.Address2,
                City = detail.City,
                State = detail.State,
                Country = detail.Country,
                CountryCode = detail.CountryCode,
                PostalCode = detail.PostalCode
            },

            Geo = new GeoCoordinates
            {
                Latitude = detail.Latitude,
                Longitude = detail.Longitude
            },

            Phone = detail.Telephone,
            Email = detail.Email,
            Website = detail.Website,
            CheckInTime = detail.CheckIn,
            CheckOutTime = detail.CheckOut,
            Languages = detail.Languages ?? new List<string>(),

            Images = (detail.Images ?? new List<StubaHotelImage>())
                .Select(img => new HotelImage
                {
                    Url = img.ImageUrl,
                    Caption = img.ImageCaption,
                    IsMain = img.IsMain
                })
                .ToList(),

            Facilities = detail.Facilities ?? new List<string>()
        };
    }

    /// <summary>
    /// Computes the SHA-256 hash of the hotel's stable content fields.
    /// Audit fields (ContentHash, LastSyncedUtc) are intentionally excluded
    /// so that a re-sync of unchanged data produces the same hash.
    /// </summary>
    public static string ComputeContentHash(HotelStaticProfile profile)
    {
        // Project only the stable content fields (no audit fields)
        var hashSource = new
        {
            profile.Provider,
            profile.ProviderHotelId,
            profile.Name,
            profile.ShortDescription,
            profile.LongDescription,
            profile.StarRating,
            profile.Category,
            profile.Address,
            profile.Geo,
            profile.Phone,
            profile.Email,
            profile.Website,
            profile.CheckInTime,
            profile.CheckOutTime,
            // Sort lists for deterministic ordering
            Languages = profile.Languages.OrderBy(l => l).ToList(),
            Images = profile.Images
                .OrderBy(i => i.Url)
                .Select(i => new { i.Url, i.Caption, i.IsMain })
                .ToList(),
            Facilities = profile.Facilities.OrderBy(f => f).ToList()
        };

        var json = JsonSerializer.Serialize(hashSource, _stableOpts);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int ParseStarRating(string value)
    {
        if (int.TryParse(value, out var i))
            return Math.Clamp(i, 0, 5);

        // Handle half-star strings like "4.5" by rounding
        if (double.TryParse(value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
            return Math.Clamp((int)Math.Round(d), 0, 5);

        return 0;
    }
}
