using HotelAPIMiddleware.Common;
using HotelAPIMiddleware.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Services;
using HotelAPIMiddleware.StaticHotels.Store;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.StaticHotels.Controllers;

/// <summary>
/// Endpoints for managing STUBA hotel static content.
/// Route prefix: /api/static-hotels/stuba
/// </summary>
[ApiController]
[Route("api/static-hotels/stuba")]
[Produces("application/json")]
public sealed class HotelStaticController : ControllerBase
{
    private const string ProviderName = "STUBA";

    private readonly IHotelStaticSyncService _syncService;
    private readonly IHotelStaticDataFetchService _fetchService;
    private readonly IHotelStaticStore _store;
    private readonly ILogger<HotelStaticController> _logger;

    public HotelStaticController(
        IHotelStaticSyncService syncService,
        IHotelStaticDataFetchService fetchService,
        IHotelStaticStore store,
        ILogger<HotelStaticController> logger)
    {
        _syncService = syncService;
        _fetchService = fetchService;
        _store = store;
        _logger = logger;
    }

    // ── FETCH endpoint (full orchestrated pipeline) ────────────────────────────

    /// <summary>
    /// POST /api/static-hotels/stuba/fetch-by-region
    ///
    /// Runs the full pipeline for a STUBA search region in a single call:
    ///   1. getAllSearchRegionsByCountry → cities in the region
    ///   2. RegionSearch (today's arrival date) → available hotel IDs per city
    ///   3. getAllHotelsDetailsByHotelIds → hotel detail with images + descriptions
    ///   4. Save each hotel as {hotelId}.json (create if new, overwrite if changed)
    /// </summary>
    [HttpPost("fetch-by-region")]
    [ProducesResponseType(typeof(ApiResponse<SyncSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> FetchByRegion(
        [FromBody] FetchByRegionRequest request,
        CancellationToken ct)
    {
        if (request.RegionId <= 0)
            return BadRequest(ApiResponse<object>.Fail("RegionId must be a positive integer."));

        if (string.IsNullOrWhiteSpace(request.Nationality))
            return BadRequest(ApiResponse<object>.Fail("Nationality is required (e.g. \"GB\")."));

        if (request.Nights <= 0)
            return BadRequest(ApiResponse<object>.Fail("Nights must be at least 1."));

        var rooms = request.Rooms?.Any() == true
            ? request.Rooms
            : new List<StubaRoom> { new() { Adult = 2 } };

        try
        {
            _logger.LogInformation(
                "fetch-by-region initiated: regionId={RegionId}, nationality={Nat}, nights={Nights}",
                request.RegionId, request.Nationality, request.Nights);

            var summary = await _fetchService.FetchByRegionAsync(
                request.RegionId, request.Nationality, request.Nights, rooms, ct);

            return Ok(ApiResponse<SyncSummary>.Ok(summary,
                $"Fetch completed. Created={summary.Created}, Updated={summary.Updated}, " +
                $"Skipped={summary.Skipped}, Failed={summary.Failed}"));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest,
                ApiResponse<object>.Fail("Fetch cancelled by client."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fetch-by-region failed for regionId={RegionId}", request.RegionId);
            return StatusCode(500, ApiResponse<object>.Fail(
                "Fetch failed due to an internal error.", new[] { ex.Message }));
        }
    }

    // ── SYNC endpoints ────────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/static-hotels/stuba/sync/hotel/{hotelId}
    ///
    /// Force-refreshes a single hotel's static profile from STUBA.
    /// </summary>
    [HttpPost("sync/hotel/{hotelId}")]
    [ProducesResponseType(typeof(ApiResponse<SyncSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncHotel(
        [FromRoute] string hotelId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hotelId))
            return BadRequest(ApiResponse<object>.Fail("hotelId is required."));

        try
        {
            _logger.LogInformation("Single hotel sync initiated for hotelId={HotelId}", hotelId);
            var summary = await _syncService.SyncHotelAsync(hotelId, ct);
            return Ok(ApiResponse<SyncSummary>.Ok(summary,
                summary.Failed == 0
                    ? $"Hotel {hotelId} synced successfully."
                    : $"Hotel {hotelId} sync failed."));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest,
                ApiResponse<object>.Fail("Sync cancelled by client."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Single hotel sync failed for hotelId={HotelId}", hotelId);
            return StatusCode(500, ApiResponse<object>.Fail(
                $"Hotel sync for '{hotelId}' failed.", new[] { ex.Message }));
        }
    }

    // ── READ endpoints ────────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/static-hotels/stuba/hotels/{hotelId}
    ///
    /// Returns the full stored static profile for a hotel.
    /// </summary>
    [HttpGet("hotels/{hotelId}")]
    [ProducesResponseType(typeof(ApiResponse<HotelStaticProfile>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHotel(
        [FromRoute] string hotelId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hotelId))
            return BadRequest(ApiResponse<object>.Fail("hotelId is required."));

        var profile = await _store.GetHotelAsync(ProviderName, hotelId, ct);

        if (profile is null)
            return NotFound(ApiResponse<object>.Fail(
                $"Hotel '{hotelId}' not found. Run fetch-by-region first."));

        return Ok(ApiResponse<HotelStaticProfile>.Ok(profile));
    }

    /// <summary>
    /// GET /api/static-hotels/stuba/hotels?page=1&amp;pageSize=50
    ///
    /// Returns a paginated list of hotel index entries (lightweight metadata).
    /// </summary>
    [HttpGet("hotels")]
    [ProducesResponseType(typeof(ApiResponse<HotelPageResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListHotels(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var result = await _store.ListHotelsAsync(ProviderName, page, pageSize, ct);
        return Ok(ApiResponse<HotelPageResult>.Ok(result));
    }

    /// <summary>
    /// GET /api/static-hotels/stuba/index
    ///
    /// Returns the raw index file with all hotel metadata.
    /// </summary>
    [HttpGet("index")]
    [ProducesResponseType(typeof(ApiResponse<HotelIndexFile>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIndex(CancellationToken ct)
    {
        var index = await _store.LoadIndexAsync(ProviderName, ct);
        return Ok(ApiResponse<HotelIndexFile>.Ok(index,
            $"Index loaded. Total hotels: {index.TotalHotels}"));
    }
}

/// <summary>
/// Request body for POST /api/static-hotels/stuba/fetch-by-region
/// </summary>
public sealed class FetchByRegionRequest
{
    /// <summary>STUBA search region ID (country or sub-region level).</summary>
    public int RegionId { get; set; }

    /// <summary>2-letter nationality code for availability search, e.g. "GB".</summary>
    public string Nationality { get; set; } = "GB";

    /// <summary>Number of nights. Defaults to 1.</summary>
    public int Nights { get; set; } = 1;

    /// <summary>Room configuration. Defaults to one room with 2 adults.</summary>
    public List<StubaRoom>? Rooms { get; set; }
}
