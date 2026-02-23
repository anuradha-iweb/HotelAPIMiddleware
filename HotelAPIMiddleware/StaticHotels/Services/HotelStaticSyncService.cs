using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.StaticHotels.Mappings;
using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba;
using HotelAPIMiddleware.StaticHotels.Store;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.StaticHotels.Services;

/// <summary>
/// Core sync orchestrator for STUBA hotel static data.
///
/// Sync algorithm per hotel:
///   1. Fetch detail from STUBA via <see cref="IStubaStaticClient.GetHotelDetailAsync"/>
///   2. Map to <see cref="HotelStaticProfile"/> using <see cref="StubaStaticHotelMapper.Map"/>
///   3. Compute SHA-256 content hash via <see cref="StubaStaticHotelMapper.ComputeContentHash"/>
///   4. Compare with stored hash from index
///      - Hash match  → skip (no write)
///      - New or hash differ → set hash + LastSyncedUtc → atomic file write → update index entry
///   5. After all hotels: save index.json atomically
///
/// Concurrency: a <see cref="SemaphoreSlim"/> limits concurrent STUBA calls
/// to <see cref="StaticHotelOptions.MaxConcurrency"/> (default 5).
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

    public async Task<SyncSummary> SyncAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting STUBA full hotel sync");

        var summary = BeginSummary(SyncScope.Full);
        var index = await _store.LoadIndexAsync(Provider, ct);

        var hotelIds = await _stubaClient.GetAllHotelIdsAsync(ct);
        _logger.LogInformation("Discovered {Count} hotel IDs from STUBA", hotelIds.Count);
        summary.TotalDiscovered = hotelIds.Count;

        await ProcessHotelsAsync(hotelIds, index, summary, forceUpdate: false, ct);

        index.LastFullSyncUtc = DateTime.UtcNow;
        await _store.SaveIndexAsync(Provider, index, ct);

        return FinishSummary(summary);
    }

    public async Task<SyncSummary> SyncByRegionAsync(string regionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting STUBA region sync for regionId={RegionId}", regionId);

        var summary = BeginSummary(SyncScope.Region, regionId);
        var index = await _store.LoadIndexAsync(Provider, ct);

        var hotelIds = await _stubaClient.GetHotelIdsByRegionAsync(regionId, ct);
        _logger.LogInformation(
            "Discovered {Count} hotel IDs for region {RegionId}", hotelIds.Count, regionId);
        summary.TotalDiscovered = hotelIds.Count;

        await ProcessHotelsAsync(hotelIds, index, summary, forceUpdate: false, ct);

        await _store.SaveIndexAsync(Provider, index, ct);

        return FinishSummary(summary);
    }

    public async Task<SyncSummary> SyncHotelAsync(string hotelId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting STUBA single hotel sync for hotelId={HotelId}", hotelId);

        var summary = BeginSummary(SyncScope.Single, hotelId);
        summary.TotalDiscovered = 1;

        var index = await _store.LoadIndexAsync(Provider, ct);

        // Force update regardless of hash match
        await ProcessHotelsAsync(new[] { hotelId }, index, summary, forceUpdate: true, ct);

        await _store.SaveIndexAsync(Provider, index, ct);

        return FinishSummary(summary);
    }

    // ── Core processing logic ─────────────────────────────────────────────────

    /// <summary>
    /// For each hotel ID: fetch → map → hash → compare → save-if-changed.
    /// Uses SemaphoreSlim to cap concurrent STUBA calls.
    /// </summary>
    private async Task ProcessHotelsAsync(
        IEnumerable<string> hotelIds,
        HotelIndexFile index,
        SyncSummary summary,
        bool forceUpdate,
        CancellationToken ct)
    {
        var sem = new SemaphoreSlim(_opts.MaxConcurrency, _opts.MaxConcurrency);

        // Object-lock for safely mutating summary + index from concurrent tasks
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
                throw; // propagate cancellation
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
        // 1. Fetch from STUBA
        var detail = await _stubaClient.GetHotelDetailAsync(hotelId, ct);
        if (detail is null)
        {
            _logger.LogWarning("STUBA returned no data for hotel {HotelId}", hotelId);
            lock (syncLock)
            {
                summary.Failed++;
                summary.FailedHotelIds.Add(hotelId);
            }
            return;
        }

        // 2. Map to profile (audit fields not set yet)
        var profile = StubaStaticHotelMapper.Map(detail);

        // 3. Compute content hash
        var newHash = StubaStaticHotelMapper.ComputeContentHash(profile);

        // 4. Compare with stored hash
        bool isNew;
        bool hashChanged;
        lock (syncLock)
        {
            isNew = !index.Hotels.TryGetValue(hotelId, out var existing);
            hashChanged = !isNew && existing!.ContentHash != newHash;
        }

        if (!forceUpdate && !isNew && !hashChanged)
        {
            _logger.LogDebug("Hotel {HotelId}: no changes detected — skipping", hotelId);
            lock (syncLock) { summary.Skipped++; }
            return;
        }

        // 5. Set audit fields and persist atomically
        profile.ContentHash = newHash;
        profile.LastSyncedUtc = DateTime.UtcNow;

        await _store.SaveHotelAsync(profile, ct);

        // 6. Update in-memory index entry
        var entry = new HotelIndexEntry
        {
            ProviderHotelId = hotelId,
            Provider = Provider,
            HotelName = profile.Name,
            City = profile.Address.City,
            CountryCode = profile.Address.CountryCode,
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
