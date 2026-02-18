using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Common.Helpers;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Providers.RateHawk.Dto;
using HotelAPIMiddleware.Providers.Stuba.Dto;

namespace HotelAPIMiddleware.Mappings;

public static class ProviderRequestMappers
{
    public static RateHawkRegionSearchRequest ToRateHawkRequest(UnifiedHotelSearchRequest req)
    {
        var regionId = RegionIdHelper.GetRegionIdFor(req.RegionId, HotelProvider.RateHawk, req.Providers);

        var room = req.Rooms[0];

        return new RateHawkRegionSearchRequest
        {
            CheckIn = req.CheckIn.ToString("yyyy-MM-dd"),
            CheckOut = req.CheckOut.ToString("yyyy-MM-dd"),
            Residency = (req.Residency ?? "ae").ToLowerInvariant(),
            Language = req.Language ?? "en",
            RegionId = regionId,
            Currency = req.Currency ?? "USD",
            Guests = new()
        {
            new RateHawkGuest
            {
                Adults = Math.Max(1, room.Adults),
                Children = room.ChildrenAges ?? new()
            }
        }
        };
    }

    public static StubaRegionSearchRequest ToStubaRequest(UnifiedHotelSearchRequest req)
    {
        var regionId = RegionIdHelper.GetRegionIdFor(req.RegionId, HotelProvider.Stuba, req.Providers);

        var nights = (req.CheckOut.ToDateTime(TimeOnly.MinValue) - req.CheckIn.ToDateTime(TimeOnly.MinValue)).Days;
        if (nights <= 0) nights = 1;

        return new StubaRegionSearchRequest
        {
            Nationality = (req.Nationality ?? "GB").ToUpperInvariant(),
            Timeout = req.TimeoutSeconds <= 0 ? 30 : req.TimeoutSeconds,
            RegionId = regionId,
            ArrivalDate = req.CheckIn.ToString("yyyy-MM-dd"),
            Nights = nights,
            Rooms = req.Rooms.Select(r => new StubaRoom
            {
                Adult = Math.Max(1, r.Adults),
                Child = r.ChildrenAges?.Count > 0 ? r.ChildrenAges.Count : null,
                ChildAges = r.ChildrenAges?.Count > 0 ? r.ChildrenAges : null
            }).ToList()
        };
    }
}
