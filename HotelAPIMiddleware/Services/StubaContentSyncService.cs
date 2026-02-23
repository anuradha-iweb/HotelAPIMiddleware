using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Providers.Stuba.StaticContent.Dto;
using Microsoft.Extensions.Options;

namespace HotelAPIMiddleware.Services;

/// <summary>
/// Syncs Stuba static hotel content using the Stuba Static Content API.
///
/// Flow:
///   1. For each RegionId call getAllHotelsListBySearchRegion → collect hotel IDs.
///   2. De-duplicate all hotel IDs.
///   3. Chunk into batches of ≤100 and call getAllHotelsDetailsByHotelIds per batch.
///   4. For each returned hotel detail, compute a SHA-256 hash of the content JSON.
///   5. Compare against the hash stored in the existing hotel file.
///   6. Only write to disk if the content is new or the hash changed.
///   7. Return a summary (new / updated / unchanged counts, errors).
///
/// Storage layout (under <see cref="StubaContentOptions.DataDirectory"/>):
///   hotels/{hotelId}.json  – JSON wrapper with metadata + raw hotel content
///   sync-state.json        – metadata about the last completed sync
/// </summary>
public class StubaContentSyncService
{
    private const int BatchSize = 100;
    private const string HotelsSubDir = "hotels";
    private const string SyncStateFile = "sync-state.json";

    private readonly IHttpClientFactory _httpFactory;
    private readonly StubaContentOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<StubaContentSyncService> _logger;

    private static readonly JsonSerializerOptions _writeIndented = new() { WriteIndented = true };

    public StubaContentSyncService(
        IHttpClientFactory httpFactory,
        IOptions<ProviderOptions> providerOptions,
        IWebHostEnvironment env,
        ILogger<StubaContentSyncService> logger)
    {
        _httpFactory = httpFactory;
        _options = providerOptions.Value.StubaContent;
        _env = env;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Run a full sync for the supplied region IDs (or the configured defaults).
    /// </summary>
    public async Task<StubaContentSyncResult> SyncAsync(
        StubaContentSyncRequest request,
        CancellationToken ct)
    {
        var result = new StubaContentSyncResult { SyncStartedAt = DateTime.UtcNow };

        var regionIds = (request.RegionIds is { Count: > 0 })
            ? request.RegionIds
            : _options.DefaultRegionIds;

        result.RegionIds = regionIds;
        result.RegionsProcessed = regionIds.Count;

        if (regionIds.Count == 0)
        {
            result.Errors.Add(
                "No region IDs provided and no DefaultRegionIds configured in " +
                "appsettings.json (Providers:StubaContent:DefaultRegionIds). " +
                "Pass RegionIds in the request body or populate the config list.");
            result.SyncCompletedAt = DateTime.UtcNow;
            return result;
        }

        EnsureDirectoriesExist();

        // --- Step 1: collect all hotel IDs from every region -----------------
        var allHotelIds = new HashSet<int>();

        foreach (var regionId in regionIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var ids = await FetchHotelIdsForRegionAsync(regionId, ct);
                _logger.LogInformation("Region {RegionId}: {Count} hotels found", regionId, ids.Count);
                foreach (var id in ids) allHotelIds.Add(id);
            }
            catch (Exception ex)
            {
                var msg = $"Region {regionId}: {ex.Message}";
                result.Errors.Add(msg);
                _logger.LogError(ex, "Error fetching hotel list for region {RegionId}", regionId);
            }
        }

        result.TotalHotelsFound = allHotelIds.Count;
        _logger.LogInformation(
            "Total unique hotels across all regions: {Total}", allHotelIds.Count);

        // --- Step 2: process in batches of 100 --------------------------------
        var batches = allHotelIds.Chunk(BatchSize).ToList();
        _logger.LogInformation(
            "Processing {Total} hotels in {Batches} batch(es)", allHotelIds.Count, batches.Count);

        for (var batchIdx = 0; batchIdx < batches.Count; batchIdx++)
        {
            ct.ThrowIfCancellationRequested();
            var batch = batches[batchIdx];

            try
            {
                var hotelJsonElements = await FetchHotelDetailsAsync(
                    batch.ToList(), request.IncludeImages, request.IncludeDescription, ct);

                foreach (var hotelElement in hotelJsonElements)
                {
                    result.TotalHotelsProcessed++;
                    try
                    {
                        var saveResult = await ProcessAndSaveHotelAsync(hotelElement);
                        switch (saveResult)
                        {
                            case HotelSaveOutcome.New:       result.NewHotels++;       break;
                            case HotelSaveOutcome.Updated:   result.UpdatedHotels++;   break;
                            case HotelSaveOutcome.Unchanged: result.UnchangedHotels++; break;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Hotel save error (batch {batchIdx + 1}): {ex.Message}");
                        _logger.LogError(ex, "Error saving hotel in batch {Batch}", batchIdx + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                var sampleIds = string.Join(", ", batch.Take(5));
                var msg = $"Batch {batchIdx + 1} (sample IDs: {sampleIds}…): {ex.Message}";
                result.Errors.Add(msg);
                _logger.LogError(ex, "Error processing batch {Batch}", batchIdx + 1);
            }
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        await SaveSyncStateAsync(result, ct);

        _logger.LogInformation(
            "Sync complete. New={New} Updated={Updated} Unchanged={Unchanged} Errors={Errors}",
            result.NewHotels, result.UpdatedHotels, result.UnchangedHotels, result.Errors.Count);

        return result;
    }

    /// <summary>
    /// Retrieves a stored hotel record by hotel ID. Returns null if not found.
    /// </summary>
    public async Task<JsonDocument?> GetStoredHotelAsync(int hotelId)
    {
        var path = HotelFilePath(hotelId);
        if (!File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonDocument.Parse(json);
    }

    // -------------------------------------------------------------------------
    // Private helpers – API calls
    // -------------------------------------------------------------------------

    private async Task<List<int>> FetchHotelIdsForRegionAsync(int regionId, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("StubaContentClient");

        var body = new StubaHotelsListByRegionRequest
        {
            Authority = BuildAuthority(),
            RegionId = regionId
        };

        var response = await PostJsonAsync(
            http, "webapi/staticData/getAllHotelsListBySearchRegion", body, ct);

        return ExtractHotelIdsFromListResponse(response);
    }

    private async Task<List<JsonElement>> FetchHotelDetailsAsync(
        List<int> hotelIds, bool includeImages, bool includeDescription, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("StubaContentClient");

        var body = new StubaHotelDetailsRequest
        {
            Authority = BuildAuthority(),
            HotelIds = hotelIds,
            Options = new StubaHotelDetailsOptions
            {
                IncludeImages = includeImages,
                IncludeDescription = includeDescription
            }
        };

        var response = await PostJsonAsync(
            http, "webapi/staticData/getAllHotelsDetailsByHotelIds", body, ct);

        return ExtractHotelElementsFromDetailsResponse(response);
    }

    private static async Task<string> PostJsonAsync<T>(
        HttpClient http, string relativeUrl, T body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(relativeUrl, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Stuba Content API returned {(int)response.StatusCode} for {relativeUrl}: {responseBody}");

        return responseBody;
    }

    // -------------------------------------------------------------------------
    // Private helpers – response parsing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extracts hotel IDs from the getAllHotelsListBySearchRegion response.
    /// Handles both top-level array and common wrapper object shapes.
    /// </summary>
    private static List<int> ExtractHotelIdsFromListResponse(string json)
    {
        var ids = new List<int>();
        if (string.IsNullOrWhiteSpace(json)) return ids;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Shape 1: root is an array of hotel objects
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var id = TryExtractHotelId(item);
                if (id.HasValue) ids.Add(id.Value);
            }
            return ids;
        }

        // Shape 2: root is an object; look for a common list property
        var listCandidates = new[] { "Hotels", "hotels", "HotelList", "hotelList", "Data", "data", "Items", "items", "Result", "result" };
        foreach (var candidate in listCandidates)
        {
            if (root.TryGetProperty(candidate, out var list) && list.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in list.EnumerateArray())
                {
                    var id = TryExtractHotelId(item);
                    if (id.HasValue) ids.Add(id.Value);
                }
                return ids;
            }
        }

        return ids;
    }

    /// <summary>
    /// Extracts hotel JsonElements from the getAllHotelsDetailsByHotelIds response.
    /// Returns them as cloned elements so the underlying JsonDocument can be disposed.
    /// </summary>
    private static List<JsonElement> ExtractHotelElementsFromDetailsResponse(string json)
    {
        var elements = new List<JsonElement>();
        if (string.IsNullOrWhiteSpace(json)) return elements;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        IEnumerable<JsonElement>? hotelArray = null;

        if (root.ValueKind == JsonValueKind.Array)
        {
            hotelArray = root.EnumerateArray();
        }
        else
        {
            var listCandidates = new[] { "Hotels", "hotels", "HotelDetails", "hotelDetails", "Data", "data", "Items", "items", "Result", "result" };
            foreach (var candidate in listCandidates)
            {
                if (root.TryGetProperty(candidate, out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    hotelArray = list.EnumerateArray();
                    break;
                }
            }
        }

        if (hotelArray is null) return elements;

        // Clone each element so it survives the using block
        foreach (var el in hotelArray)
            elements.Add(el.Clone());

        return elements;
    }

    /// <summary>
    /// Tries common field names to extract an integer hotel ID from a JSON element.
    /// </summary>
    private static int? TryExtractHotelId(JsonElement element)
    {
        var candidates = new[] { "HotelId", "hotelId", "Id", "id", "hotel_id", "HotelID" };
        foreach (var name in candidates)
        {
            if (!element.TryGetProperty(name, out var prop)) continue;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var intVal))
                return intVal;

            if (prop.ValueKind == JsonValueKind.String &&
                int.TryParse(prop.GetString(), out var parsedVal))
                return parsedVal;
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // Private helpers – storage
    // -------------------------------------------------------------------------

    private enum HotelSaveOutcome { New, Updated, Unchanged }

    private async Task<HotelSaveOutcome> ProcessAndSaveHotelAsync(JsonElement hotelElement)
    {
        var hotelId = TryExtractHotelId(hotelElement)
            ?? throw new InvalidOperationException("Cannot determine HotelId from detail element.");

        // Serialize the content once for hashing and potential storage
        var contentJson = JsonSerializer.Serialize(hotelElement);
        var newHash = ComputeSha256(contentJson);

        var filePath = HotelFilePath(hotelId);
        var isNew = !File.Exists(filePath);
        var firstSeen = DateTime.UtcNow;

        if (!isNew)
        {
            // Load only the metadata fields to check the hash (avoid full re-parse overhead)
            var existingRaw = await File.ReadAllTextAsync(filePath);
            using var existingDoc = JsonDocument.Parse(existingRaw);
            var existingRoot = existingDoc.RootElement;

            if (existingRoot.TryGetProperty("ContentHash", out var storedHashProp) &&
                storedHashProp.GetString() == newHash)
            {
                return HotelSaveOutcome.Unchanged;
            }

            // Preserve the original FirstSeen timestamp
            if (existingRoot.TryGetProperty("FirstSeen", out var firstSeenProp) &&
                firstSeenProp.TryGetDateTime(out var originalFirstSeen))
            {
                firstSeen = originalFirstSeen;
            }
        }

        // Build the wrapper record and write it to disk
        var record = new
        {
            HotelId = hotelId,
            ContentHash = newHash,
            LastUpdated = DateTime.UtcNow,
            FirstSeen = firstSeen,
            Content = hotelElement
        };

        var recordJson = JsonSerializer.Serialize(record, _writeIndented);
        await File.WriteAllTextAsync(filePath, recordJson);

        return isNew ? HotelSaveOutcome.New : HotelSaveOutcome.Updated;
    }

    private async Task SaveSyncStateAsync(StubaContentSyncResult result, CancellationToken ct)
    {
        var statePath = Path.Combine(HotelsBaseDir(), SyncStateFile);
        var state = new
        {
            result.SyncStartedAt,
            result.SyncCompletedAt,
            result.RegionIds,
            result.TotalHotelsFound,
            result.TotalHotelsProcessed,
            result.NewHotels,
            result.UpdatedHotels,
            result.UnchangedHotels,
            ErrorCount = result.Errors.Count
        };
        var json = JsonSerializer.Serialize(state, _writeIndented);
        await File.WriteAllTextAsync(statePath, json, ct);
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(HotelsDir());
    }

    private string HotelsBaseDir() =>
        Path.Combine(_env.ContentRootPath, _options.DataDirectory);

    private string HotelsDir() =>
        Path.Combine(HotelsBaseDir(), HotelsSubDir);

    private string HotelFilePath(int hotelId) =>
        Path.Combine(HotelsDir(), $"{hotelId}.json");

    // -------------------------------------------------------------------------
    // Private helpers – hashing
    // -------------------------------------------------------------------------

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // -------------------------------------------------------------------------
    // Private helpers – config
    // -------------------------------------------------------------------------

    private StubaContentAuthority BuildAuthority() => new()
    {
        Org = _options.Org,
        User = _options.User,
        Password = _options.Password
    };
}
