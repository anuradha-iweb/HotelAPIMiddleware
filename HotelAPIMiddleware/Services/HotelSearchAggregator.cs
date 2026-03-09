using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Providers.Interfaces;
using HotelAPIMiddleware.StaticHotels.Store;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.Services;

public class HotelSearchAggregator
{
    private readonly IEnumerable<IHotelProvider> _providers;
    private readonly IMemoryCache _cache;
    private readonly IHotelStaticStore _hotelStaticStore;
    private readonly SearchTuningOptions _searchTuning;
    private readonly ILogger<HotelSearchAggregator> _logger;

    public HotelSearchAggregator(
        IEnumerable<IHotelProvider> providers,
        IMemoryCache cache,
        IHotelStaticStore hotelStaticStore,
        IOptions<SearchTuningOptions> searchTuning,
        ILogger<HotelSearchAggregator> logger)
    {
        _providers = providers;
        _cache = cache;
        _hotelStaticStore = hotelStaticStore;
        _searchTuning = searchTuning.Value;
        _logger = logger;
    }

    public async Task<HotelSearchResponse> SearchAsync(UnifiedHotelSearchRequest request, CancellationToken ct)
    {
        var totalSw = Stopwatch.StartNew();
        var response = new HotelSearchResponse
        {
            SearchId = Guid.NewGuid().ToString()
        };

        var selected = _providers
            .Where(p => request.Providers.Contains(p.Provider))
            .ToList();

        if (selected.Count == 0)
        {
            response.Providers.Add(new ProviderExecutionInfo
            {
                Provider = "NONE",
                Success = false,
                Error = "No providers selected."
            });
            return response;
        }

        var tasks = selected.Select(async p =>
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await p.SearchAsync(request, ct);
                var providerName = p.Provider.ToString().ToUpperInvariant();
                var hotels = result.Hotels ?? new List<HotelResult>();
                var originalCount = hotels.Count;

                foreach (var hotel in hotels)
                {
                    hotel.Provider = providerName;

                    foreach (var room in hotel.Rooms)
                    {
                        foreach (var rate in room.Rates)
                        {
                            rate.Provider = providerName;
                        }
                    }
                }

                var maxHotels = Math.Max(1, _searchTuning.MaxHotelsPerProvider);
                var truncatedCount = 0;
                if (hotels.Count > maxHotels)
                {
                    truncatedCount = hotels.Count - maxHotels;
                    hotels = hotels.Take(maxHotels).ToList();
                }

                var staticEnrichmentSw = Stopwatch.StartNew();
                var enrichedCount = await EnrichStaticDataAsync(providerName, hotels, ct);
                staticEnrichmentSw.Stop();

                sw.Stop();

                _logger.LogInformation(
                    "Search provider={Provider} success={Success} providerMs={ProviderMs} staticEnrichmentMs={EnrichmentMs} originalCount={OriginalCount} returnedCount={ReturnedCount} truncatedCount={TruncatedCount} enrichedCount={EnrichedCount}",
                    providerName,
                    result.Success,
                    sw.ElapsedMilliseconds,
                    staticEnrichmentSw.ElapsedMilliseconds,
                    originalCount,
                    hotels.Count,
                    truncatedCount,
                    enrichedCount);

                if (truncatedCount > 0)
                {
                    _logger.LogInformation(
                        "Search results truncated provider={Provider} truncatedCount={TruncatedCount} maxHotelsPerProvider={MaxHotelsPerProvider}",
                        providerName,
                        truncatedCount,
                        maxHotels);
                }

                return new ProviderSearchTaskResult
                {
                    Info = new ProviderExecutionInfo
                    {
                        Provider = providerName,
                        Success = result.Success,
                        TimeMs = (int)sw.ElapsedMilliseconds,
                        Error = result.ErrorMessage
                    },
                    Hotels = hotels
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Search provider failed provider={Provider} elapsedMs={ElapsedMs}", p.Provider, sw.ElapsedMilliseconds);

                return new ProviderSearchTaskResult
                {
                    Info = new ProviderExecutionInfo
                    {
                        Provider = p.Provider.ToString().ToUpperInvariant(),
                        Success = false,
                        TimeMs = (int)sw.ElapsedMilliseconds,
                        Error = ex.Message
                    },
                    Hotels = new List<HotelResult>()
                };
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);

        response.Providers = results.Select(x => x.Info).ToList();
        response.Hotels = results.SelectMany(x => x.Hotels).ToList();

        foreach (var hotel in response.Hotels)
        {
            hotel.UniqueId = $"hotel_{Guid.NewGuid()}";
        }

        var cacheId = $"hotel_search_cache_{response.SearchId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        response.CacheId = cacheId;
        _cache.Set(cacheId, response, TimeSpan.FromMinutes(30));

        totalSw.Stop();
        _logger.LogInformation(
            "Search completed searchId={SearchId} providerCount={ProviderCount} hotelCount={HotelCount} totalMs={TotalMs}",
            response.SearchId,
            response.Providers.Count,
            response.Hotels.Count,
            totalSw.ElapsedMilliseconds);

        return response;
    }

    private async Task<int> EnrichStaticDataAsync(string provider, List<HotelResult> hotels, CancellationToken ct)
    {
        if (!provider.Equals("STUBA", StringComparison.OrdinalIgnoreCase) || hotels.Count == 0)
            return 0;

        var hotelIds = hotels
            .Select(h => !string.IsNullOrWhiteSpace(h.StaticDataId) ? h.StaticDataId : (string.IsNullOrWhiteSpace(h.Id) ? h.HotelId : h.Id))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (hotelIds.Count == 0)
            return 0;

        var maxConcurrency = Math.Max(1, _searchTuning.StaticEnrichmentConcurrency);
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var staticDataMap = new ConcurrentDictionary<string, JsonElement?>(StringComparer.OrdinalIgnoreCase);

        var fetchTasks = hotelIds.Select(async hotelId =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                staticDataMap[hotelId] = await ResolveStaticDataAsync(provider, hotelId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Static enrichment failed provider={Provider} hotelId={HotelId}", provider, hotelId);
                staticDataMap[hotelId] = null;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(fetchTasks);

        var enrichedCount = 0;
        foreach (var hotel in hotels)
        {
            var staticId = !string.IsNullOrWhiteSpace(hotel.StaticDataId)
                ? hotel.StaticDataId
                : (string.IsNullOrWhiteSpace(hotel.Id) ? hotel.HotelId : hotel.Id);

            if (string.IsNullOrWhiteSpace(staticId))
                continue;

            if (!staticDataMap.TryGetValue(staticId, out var staticData))
                continue;

            hotel.StaticData = staticData;
            if (staticData.HasValue)
                enrichedCount++;
        }

        _logger.LogInformation(
            "Static enrichment summary provider={Provider} requestedIds={RequestedIds} enrichedCount={EnrichedCount} missingCount={MissingCount} concurrency={Concurrency}",
            provider,
            hotelIds.Count,
            enrichedCount,
            hotelIds.Count - enrichedCount,
            maxConcurrency);

        return enrichedCount;
    }

    private async Task<JsonElement?> ResolveStaticDataAsync(string provider, string hotelId, CancellationToken ct)
    {
        if (!provider.Equals("STUBA", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.IsNullOrWhiteSpace(hotelId))
            return null;

        var profile = await _hotelStaticStore.GetHotelAsync(provider, hotelId, ct);
        if (profile is null)
            return null;

        return JsonSerializer.SerializeToElement(profile);
    }

    private sealed class ProviderSearchTaskResult
    {
        public ProviderExecutionInfo Info { get; init; } = new();
        public List<HotelResult> Hotels { get; init; } = new();
    }
}
