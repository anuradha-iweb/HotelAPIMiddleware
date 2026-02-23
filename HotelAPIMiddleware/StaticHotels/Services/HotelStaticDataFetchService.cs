using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Mappings;
using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Store;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.StaticHotels.Services;

/// <summary>
/// Full pipeline:
///   1a. getAllSearchRegionsByCountry (with supplied regionId) → sub-regions (if country-level)
///   1b. getAllSearchRegionsByCountry (with each sub-region ID) → cities
///       If no sub-regions returned, cities are fetched directly with the supplied regionId.
///   2.  RegionSearch (arrival date defaults to today) → hotel IDs per city search-region
///   3.  getAllHotelsDetailsByHotelIds → hotel elements with images + descriptions
///   4.  Save each hotel as {hotelId}.json (create or overwrite on change)
/// </summary>
public sealed class HotelStaticDataFetchService : IHotelStaticDataFetchService
{
    private const string Provider = "STUBA";

    private readonly IStubaStaticClient _stubaClient;
    private readonly IHotelStaticStore _store;
    private readonly StaticHotelOptions _opts;
    private readonly ILogger<HotelStaticDataFetchService> _logger;

    public HotelStaticDataFetchService(
        IStubaStaticClient stubaClient,
        IHotelStaticStore store,
        IOptions<StaticHotelOptions> opts,
        ILogger<HotelStaticDataFetchService> logger)
    {
        _stubaClient = stubaClient;
        _store = store;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<SyncSummary> FetchByRegionAsync(
        int regionId,
        string nationality,
        int nights,
        IEnumerable<StubaRoom> rooms,
        DateOnly? arrivalDate = null,
        CancellationToken ct = default)
    {
        var roomList = rooms.ToList();
        var summary = BeginSummary(regionId);

        var effectiveArrivalDate = arrivalDate ?? DateOnly.FromDateTime(DateTime.Today);

        _logger.LogInformation(
            "FetchByRegion started: regionId={RegionId}, nationality={Nat}, nights={Nights}, arrivalDate={ArrivalDate}",
            regionId, nationality, nights, effectiveArrivalDate);

        // ── Step 1: Resolve cities (two-level when regionId is country/area level) ────
        // First try to interpret regionId as a high-level region to get sub-regions
        // (getAllSearchRegionsByCountry returns RegionId/RegionName at country level
        //  and CityId/CityName at region/city level).
        var subRegions = await _stubaClient.GetRegionsByCountryAsync(regionId, ct);

        List<StubaSearchCity> cities;

        if (subRegions.Count > 0)
        {
            // regionId is at country/continent level — fetch cities per sub-region
            _logger.LogInformation(
                "Found {Count} sub-regions for regionId={RegionId}; resolving cities per sub-region",
                subRegions.Count, regionId);

            var allCities = new List<StubaSearchCity>();
            foreach (var subRegion in subRegions)
            {
                ct.ThrowIfCancellationRequested();
                var citiesInSubRegion = await _stubaClient.GetCitiesByRegionAsync(subRegion.RegionId, ct);
                allCities.AddRange(citiesInSubRegion);

                if (_opts.PageDelayMs > 0)
                    await Task.Delay(_opts.PageDelayMs, ct);
            }
            cities = allCities;
        }
        else
        {
            // regionId is already at region/city level — get cities directly
            cities = (await _stubaClient.GetCitiesByRegionAsync(regionId, ct)).ToList();
        }

        if (cities.Count == 0)
        {
            _logger.LogWarning("No cities found for regionId={RegionId}", regionId);
            return FinishSummary(summary);
        }

        _logger.LogInformation(
            "Found {Count} cities for regionId={RegionId}", cities.Count, regionId);

        // ── Step 2: Collect hotel IDs via RegionSearch ────────────────────────
        var hotelIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var city in cities)
        {
            ct.ThrowIfCancellationRequested();

            // Use CityWiseRegionList search regions; fall back to CityId directly.
            var searchRegionIds = city.CityWiseRegionList?.Select(r => r.RegionId).ToList()
                                  ?? new List<int> { city.CityId };

            foreach (var searchRegionId in searchRegionIds)
            {
                ct.ThrowIfCancellationRequested();

                var ids = await _stubaClient.SearchHotelIdsByRegionAsync(
                    searchRegionId, nationality, nights, roomList, effectiveArrivalDate, ct);

                foreach (var id in ids)
                    hotelIds.Add(id);

                if (_opts.PageDelayMs > 0)
                    await Task.Delay(_opts.PageDelayMs, ct);
            }
        }

        summary.TotalDiscovered = hotelIds.Count;
        _logger.LogInformation(
            "RegionSearch completed: {Total} unique hotel IDs to fetch details for", hotelIds.Count);

        if (hotelIds.Count == 0)
            return FinishSummary(summary);

        // ── Step 3 & 4: Fetch details one-by-one and save ────────────────────
        var index = await _store.LoadIndexAsync(Provider, ct);
        var syncLock = new object();
        var sem = new SemaphoreSlim(_opts.MaxConcurrency, _opts.MaxConcurrency);

        var tasks = hotelIds.Select(async hotelId =>
        {
            await sem.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                var element = await _stubaClient.GetHotelDetailAsync(hotelId, ct);

                if (element is null)
                {
                    lock (syncLock)
                    {
                        summary.Failed++;
                        summary.FailedHotelIds.Add(hotelId);
                    }
                    return;
                }

                await ProcessHotelElementAsync(element, index, summary, syncLock, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing hotel detail for hotelId={HotelId}", hotelId);
                lock (syncLock)
                {
                    summary.Failed++;
                    summary.FailedHotelIds.Add(hotelId);
                }
            }
            finally
            {
                sem.Release();
            }
        });

        await Task.WhenAll(tasks);

        await _store.SaveIndexAsync(Provider, index, ct);

        _logger.LogInformation(
            "FetchByRegion done: Created={C}, Updated={U}, Skipped={S}, Failed={F}",
            summary.Created, summary.Updated, summary.Skipped, summary.Failed);

        return FinishSummary(summary);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task ProcessHotelElementAsync(
        StaticHotels.Providers.Stuba.Dto.StubaHotelElement element,
        HotelIndexFile index,
        SyncSummary summary,
        object syncLock,
        CancellationToken ct)
    {
        var hotelId = element.Id.ToString();

        var profile = StubaStaticHotelMapper.Map(element);
        var newHash = StubaStaticHotelMapper.ComputeContentHash(profile);

        bool isNew;
        bool hashChanged;
        lock (syncLock)
        {
            isNew = !index.Hotels.TryGetValue(hotelId, out var existing);
            hashChanged = !isNew && existing!.ContentHash != newHash;
        }

        if (!isNew && !hashChanged)
        {
            _logger.LogDebug("Hotel {HotelId} ({Name}): no changes — skipping", hotelId, element.Name);
            lock (syncLock) { summary.Skipped++; }
            return;
        }

        profile.ContentHash = newHash;
        profile.LastSyncedUtc = DateTime.UtcNow;

        await _store.SaveHotelAsync(profile, ct);

        var entry = new HotelIndexEntry
        {
            ProviderHotelId = hotelId,
            Provider = Provider,
            HotelName = profile.Name,
            City = profile.Address.City,
            CountryCode = profile.Address.Country,
            RelativeFilePath = $"{Provider.ToLowerInvariant()}/hotels/{hotelId}.json",
            ContentHash = newHash,
            LastUpdatedUtc = profile.LastSyncedUtc
        };

        lock (syncLock)
        {
            index.Hotels[hotelId] = entry;

            if (isNew)
                summary.Created++;
            else
                summary.Updated++;
        }

        _logger.LogDebug("Hotel {HotelId} ({Name}): {Action}",
            hotelId, element.Name, isNew ? "created" : "updated");
    }

    private static SyncSummary BeginSummary(int regionId) => new()
    {
        Provider = Provider,
        Scope = SyncScope.Region,
        ScopeId = regionId.ToString(),
        StartedUtc = DateTime.UtcNow
    };

    private static SyncSummary FinishSummary(SyncSummary summary)
    {
        summary.CompletedUtc = DateTime.UtcNow;
        return summary;
    }
}
