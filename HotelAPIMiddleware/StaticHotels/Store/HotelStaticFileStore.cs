using System.Text.Json;
using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.StaticHotels.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.StaticHotels.Store;

/// <summary>
/// File-system implementation of <see cref="IHotelStaticStore"/>.
///
/// Storage layout (relative to StaticHotels:StoragePath):
///   {StoragePath}/
///     stuba/
///       hotels/
///         {hotelId}.json      ← full HotelStaticProfile
///       index.json            ← HotelIndexFile (hash + metadata)
///
/// All writes are atomic: content is written to a .tmp file first,
/// then renamed over the target, preventing partial reads.
///
/// The index file is protected by a per-provider SemaphoreSlim(1,1)
/// to guarantee exclusive writes even under concurrent sync tasks.
/// </summary>
public sealed class HotelStaticFileStore : IHotelStaticStore
{
    private readonly string _rootPath;
    private readonly ILogger<HotelStaticFileStore> _logger;
    private readonly IMemoryCache _cache;
    private readonly SearchTuningOptions _searchTuning;
    private static readonly object NotFoundSentinel = new();
    private long _cacheHitCount;
    private long _cacheMissCount;

    // One semaphore per provider to serialize index writes
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim>
        _indexLocks = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions _writeOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions _readOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HotelStaticFileStore(
        IOptions<StaticHotelOptions> opts,
        IOptions<SearchTuningOptions> searchTuning,
        IWebHostEnvironment env,
        IMemoryCache cache,
        ILogger<HotelStaticFileStore> logger)
    {
        // Resolve StoragePath relative to content root when it's not absolute
        var storagePath = opts.Value.StoragePath;
        _rootPath = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.Combine(env.ContentRootPath, storagePath);

        _searchTuning = searchTuning.Value;
        _cache = cache;
        _logger = logger;
    }

    // ── IHotelStaticStore ────────────────────────────────────────────────────

    public async Task SaveHotelAsync(HotelStaticProfile profile, CancellationToken ct = default)
    {
        var filePath = HotelFilePath(profile.Provider, profile.ProviderHotelId);
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        await AtomicWriteJsonAsync(filePath, profile, ct);
        _cache.Set(
            BuildCacheKey(profile.Provider, profile.ProviderHotelId),
            profile,
            TimeSpan.FromMinutes(Math.Max(1, _searchTuning.StaticCacheTtlMinutes)));

        _logger.LogDebug("Saved hotel {HotelId} → {File}",
            profile.ProviderHotelId, Path.GetRelativePath(_rootPath, filePath));
    }

    public async Task<HotelStaticProfile?> GetHotelAsync(
        string provider, string hotelId, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(provider, hotelId);
        if (_cache.TryGetValue(cacheKey, out object? cachedValue))
        {
            Interlocked.Increment(ref _cacheHitCount);

            if (ReferenceEquals(cachedValue, NotFoundSentinel))
            {
                _logger.LogDebug(
                    "Static profile cache hit (negative) provider={Provider} hotelId={HotelId} hitCount={HitCount} missCount={MissCount}",
                    provider,
                    hotelId,
                    _cacheHitCount,
                    _cacheMissCount);
                return null;
            }

            _logger.LogDebug(
                "Static profile cache hit provider={Provider} hotelId={HotelId} hitCount={HitCount} missCount={MissCount}",
                provider,
                hotelId,
                _cacheHitCount,
                _cacheMissCount);
            return cachedValue as HotelStaticProfile;
        }

        Interlocked.Increment(ref _cacheMissCount);
        _logger.LogDebug(
            "Static profile cache miss provider={Provider} hotelId={HotelId} hitCount={HitCount} missCount={MissCount}",
            provider,
            hotelId,
            _cacheHitCount,
            _cacheMissCount);

        var filePath = HotelFilePath(provider, hotelId);
        if (!File.Exists(filePath))
        {
            _cache.Set(
                cacheKey,
                NotFoundSentinel,
                TimeSpan.FromMinutes(Math.Max(1, _searchTuning.StaticNegativeCacheTtlMinutes)));
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            var profile = JsonSerializer.Deserialize<HotelStaticProfile>(json, _readOpts);
            if (profile is null)
            {
                _cache.Set(
                    cacheKey,
                    NotFoundSentinel,
                    TimeSpan.FromMinutes(Math.Max(1, _searchTuning.StaticNegativeCacheTtlMinutes)));
                return null;
            }

            _cache.Set(
                cacheKey,
                profile,
                TimeSpan.FromMinutes(Math.Max(1, _searchTuning.StaticCacheTtlMinutes)));

            return profile;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to read hotel file {File}", filePath);
            return null;
        }
    }

    public async Task<HotelIndexFile> LoadIndexAsync(
        string provider, CancellationToken ct = default)
    {
        var indexPath = IndexFilePath(provider);

        if (!File.Exists(indexPath))
            return new HotelIndexFile();

        try
        {
            var json = await File.ReadAllTextAsync(indexPath, ct);
            return JsonSerializer.Deserialize<HotelIndexFile>(json, _readOpts)
                   ?? new HotelIndexFile();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Could not parse existing index for provider {Provider}; starting fresh.", provider);
            return new HotelIndexFile();
        }
    }

    public async Task SaveIndexAsync(
        string provider, HotelIndexFile index, CancellationToken ct = default)
    {
        var sem = _indexLocks.GetOrAdd(provider, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            var indexPath = IndexFilePath(provider);
            Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
            await AtomicWriteJsonAsync(indexPath, index, ct);

            _logger.LogDebug("Saved index for provider {Provider}: {Total} hotels", provider, index.TotalHotels);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<HotelPageResult> ListHotelsAsync(
        string provider, int page, int pageSize, CancellationToken ct = default)
    {
        var index = await LoadIndexAsync(provider, ct);
        var allEntries = index.Hotels.Values.ToList();
        var total = allEntries.Count;
        var items = allEntries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new HotelPageResult
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string HotelFilePath(string provider, string hotelId)
    {
        var safeId = SanitizeFileName(hotelId);
        return Path.Combine(_rootPath, provider.ToLowerInvariant(), "hotels", $"{safeId}.json");
    }

    private string IndexFilePath(string provider)
    {
        return Path.Combine(_rootPath, provider.ToLowerInvariant(), "index.json");
    }

    /// <summary>
    /// Writes JSON to a .tmp file then renames it over the target.
    /// Guarantees that readers never see a partial write.
    /// </summary>
    private static async Task AtomicWriteJsonAsync<T>(
        string targetPath, T value, CancellationToken ct)
    {
        var tmpPath = targetPath + ".tmp";

        try
        {
            var json = JsonSerializer.Serialize(value, _writeOpts);
            await File.WriteAllTextAsync(tmpPath, json, ct);
            File.Move(tmpPath, targetPath, overwrite: true);
        }
        catch
        {
            // Clean up orphaned .tmp on failure
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);
            throw;
        }
    }

    private static string SanitizeFileName(string hotelId)
    {
        // Remove any chars that are invalid in file names
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(hotelId.Select(c => invalid.Contains(c) ? '_' : c));
    }

    private static string BuildCacheKey(string provider, string hotelId)
        => $"static_profile:{provider}:{hotelId}".ToLowerInvariant();
}
