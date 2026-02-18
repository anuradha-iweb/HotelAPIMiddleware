using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Common.Helpers;

public static class RegionIdHelper
{
    public static int GetRegionIdFor(
        Dictionary<string, int>? regionIds,
        HotelProvider provider,
        IEnumerable<HotelProvider> requestedProviders)
    {
        // If provider not requested, don't require its regionId
        if (!requestedProviders.Contains(provider))
            return 0;

        if (regionIds == null || regionIds.Count == 0)
            throw new ArgumentException("regionId is required.");

        var key = provider.ToString().ToLowerInvariant(); // "stuba", "ratehawk"

        if (!regionIds.TryGetValue(key, out var regionId) || regionId <= 0)
            throw new ArgumentException($"regionId.{key} is required when provider '{provider}' is selected.");

        return regionId;
    }
}
