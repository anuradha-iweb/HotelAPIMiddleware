using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.StaticHotels.Mappings;
using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba;
using HotelAPIMiddleware.StaticHotels.Store;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.StaticHotels.Services;

/// <summary>
/// Handles individual and small-batch hotel static data refreshes.
///
/// For full region-wide fetching (regions → cities → availability → details),
/// use <see cref="IHotelStaticDataFetchService"/> instead.
///
/// Sync algorithm per hotel:
///   1. Fetch detail from STUBA via getAllHotelsDetailsByHotelIds (single-ID call)
///   2. Map to <see cref="HotelStaticProfile"/>
///   3. Compute SHA-256 content hash
///   4. Compare with stored hash:
///      - Match → skip
///      - New or changed → atomic file write → update index entry
///   5. Save index atomically
/// </summary>
public sealed class HotelStaticSyncService : IHotelStaticSyncService
{
    private const string Provider = "STUBA";

    private readonly IStubaStaticClient _stubaClient;
    private readonly IHotelStaticStore _store;
    private readonly StaticHotelOptions _opts;
    private readonly ILogger<HotelStaticSyncService> _logger;

    public HotelStaticSyncService(
        IStubaStaticClient stubaClient,
        IHotelStaticStore store,
        IOptions<StaticHotelOptions> opts,
        ILogger<HotelStaticSyncService> logger)
    {
        _stubaClient = stubaClient;
        _store = store;
        _opts = opts.Value;
        _logger = logger;
    }

    // ── IHotelStaticSyncService ───────────────────────────────────────────────

    /// <summary>
    /// Not supported: STUBA's static-content API does not expose a full hotel list.
    /// Use <see cref="IHotelStaticDataFetchService.FetchByRegionAsync"/> to
    /// discover hotels via availability search.
    /// </summary>
    public Task<SyncSummary> SyncAllAsync(CancellationToken ct = default)
    {
        _logger.LogWarning(
            "SyncAllAsync is not supported. " +
            "Use POST /api/static-hotels/stuba/fetch-by-region to discover hotels via availability.");

        return Task.FromResult(new SyncSummary
        {
            Provider = Provider,
            Scope = SyncScope.Full,
            StartedUtc = DateTime.UtcNow,
            CompletedUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Syncs hotels found in a region by fetching cities and using CityId as hotel ID.
    /// For availability-based discovery (recommended), use
    /// <see cref="IHotelStaticDataFetchService.FetchByRegionAsync"/>.
    /// </summary>
    public async Task<SyncSummary> SyncByRegionAsync(string regionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Region sync for regionId={RegionId}", regionId);

        var summary = BeginSummary(SyncScope.Region, regionId);
        var index = await _store.LoadIndexAsync(Provider, ct);

        if (!int.TryParse(regionId, out var regionIdInt))
        {
            _logger.LogError("regionId must be an integer, got '{Value}'", regionId);
            return FinishSummary(summary);
        }

        var cities = await _stubaClient.GetCitiesByRegionAsync(regionIdInt, ct);
        var hotelIds = cities.Select(c => c.CityId.ToString()).ToList();

        _logger.LogInformation(
            "Region {RegionId}: found {Count} city IDs", regionId, hotelIds.Count);
        summary.TotalDiscovered = hotelIds.Count;

        await ProcessHotelsAsync(hotelIds, index, summary, forceUpdate: false, ct);

        await _store.SaveIndexAsync(Provider, index, ct);

        return FinishSummary(summary);
    }

    /// <summary>
    /// Force-refreshes a single hotel's static profile from STUBA,
    /// bypassing hash comparison (always writes).
    /// </summary>
    public async Task<SyncSummary> SyncHotelAsync(string hotelId, CancellationToken ct = default)
    {
        _logger.LogInformation("Single hotel sync for hotelId={HotelId}", hotelId);

        var summary = BeginSummary(SyncScope.Single, hotelId);
        summary.TotalDiscovered = 1;

        var index = await _store.LoadIndexAsync(Provider, ct);

        await ProcessHotelsAsync(new[] { hotelId }, index, summary, forceUpdate: true, ct);

        await _store.SaveIndexAsync(Provider, index, ct);

        return FinishSummary(summary);
    }

    // ── Core processing ───────────────────────────────────────────────────────

    private async Task ProcessHotelsAsync(
        IEnumerable<string> hotelIds,
        HotelIndexFile index,
        SyncSummary summary,
        bool forceUpdate,
        CancellationToken ct)
    {
        var sem = new SemaphoreSlim(_opts.MaxConcurrency, _opts.MaxConcurrency);
        var syncLock = new object();

        var tasks = hotelIds.Select(async hotelId =>
        {
            await sem.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                await ProcessSingleHotelAsync(hotelId, index, summary, forceUpdate, syncLock, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing hotel {HotelId}", hotelId);
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
    }

    private async Task ProcessSingleHotelAsync(
        string hotelId,
        HotelIndexFile index,
        SyncSummary summary,
        bool forceUpdate,
        object syncLock,
        CancellationToken ct)
    {
        var element = await _stubaClient.GetHotelDetailAsync(hotelId, ct);

        if (element is null)
        {
            _logger.LogWarning("No detail returned for hotel {HotelId}", hotelId);
            lock (syncLock)
            {
                summary.Failed++;
                summary.FailedHotelIds.Add(hotelId);
            }
            return;
        }

        var profile = StubaStaticHotelMapper.Map(element);
        var newHash = StubaStaticHotelMapper.ComputeContentHash(profile);

        bool isNew;
        bool hashChanged;
        lock (syncLock)
        {
            isNew = !index.Hotels.TryGetValue(hotelId, out var existing);
            hashChanged = !isNew && existing!.ContentHash != newHash;
        }

        if (!forceUpdate && !isNew && !hashChanged)
        {
            _logger.LogDebug("Hotel {HotelId}: no changes — skipping", hotelId);
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
            if (isNew) summary.Created++;
            else summary.Updated++;
        }

        _logger.LogDebug("Hotel {HotelId}: {Action}", hotelId, isNew ? "created" : "updated");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SyncSummary BeginSummary(SyncScope scope, string? scopeId = null) => new()
    {
        Provider = Provider,
        Scope = scope,
        ScopeId = scopeId,
        StartedUtc = DateTime.UtcNow
    };

    private static SyncSummary FinishSummary(SyncSummary summary)
    {
        summary.CompletedUtc = DateTime.UtcNow;
        return summary;
    }
}
