using HotelAPIMiddleware.Common;
using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Services;
using HotelAPIMiddleware.StaticHotels.Store;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.StaticHotels.Controllers;

/// <summary>
/// Endpoints for managing STUBA hotel static content.
///
/// RBAC NOTE: Sync endpoints (POST) are intended for admin/internal use only.
/// Add [Authorize(Roles = "Admin")] or API-key middleware to POST routes
/// before going to production.
///
/// Route prefix: /api/static-hotels/stuba
/// </summary>
[ApiController]
[Route("api/static-hotels/stuba")]
[Produces("application/json")]
public sealed class HotelStaticController : ControllerBase
{
    private const string ProviderName = "STUBA";

    private readonly IHotelStaticSyncService _syncService;
    private readonly IHotelStaticStore _store;
    private readonly ILogger<HotelStaticController> _logger;

    public HotelStaticController(
        IHotelStaticSyncService syncService,
        IHotelStaticStore store,
        ILogger<HotelStaticController> logger)
    {
        _syncService = syncService;
        _store = store;
        _logger = logger;
    }

    // ── SYNC endpoints (admin-only) ────────────────────────────────────────────

    /// <summary>
    /// POST /api/static-hotels/stuba/sync/all
    ///
    /// Triggers a full sync of all STUBA hotels.
    /// Discovers ALL hotel IDs via paging, then fetches + stores each one
    /// that is new or changed (SHA-256 hash comparison).
    ///
    /// Expected duration: several minutes to hours depending on hotel count.
    /// For production workloads, consider running this as a background job
    /// and polling the index endpoint for progress.
    /// </summary>
    [HttpPost("sync/all")]
    [ProducesResponseType(typeof(ApiResponse<SyncSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncAll(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Full STUBA static sync initiated via API");
            var summary = await _syncService.SyncAllAsync(ct);
            return Ok(ApiResponse<SyncSummary>.Ok(summary,
                $"Full sync completed. Created={summary.Created}, Updated={summary.Updated}, " +
                $"Skipped={summary.Skipped}, Failed={summary.Failed}"));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest,
                ApiResponse<object>.Fail("Sync cancelled by client."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Full sync failed");
            return StatusCode(500, ApiResponse<object>.Fail(
                "Sync failed due to an internal error.", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/static-hotels/stuba/sync/region/{regionId}
    ///
    /// Syncs only hotels that belong to the given STUBA destination/region.
    /// </summary>
    [HttpPost("sync/region/{regionId}")]
    [ProducesResponseType(typeof(ApiResponse<SyncSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncByRegion(
        [FromRoute] string regionId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(regionId))
            return BadRequest(ApiResponse<object>.Fail("regionId is required."));

        try
        {
            _logger.LogInformation(
                "Region STUBA static sync initiated via API for regionId={RegionId}", regionId);

            var summary = await _syncService.SyncByRegionAsync(regionId, ct);
            return Ok(ApiResponse<SyncSummary>.Ok(summary,
                $"Region sync completed for {regionId}. Created={summary.Created}, " +
                $"Updated={summary.Updated}, Skipped={summary.Skipped}, Failed={summary.Failed}"));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest,
                ApiResponse<object>.Fail("Sync cancelled by client."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Region sync failed for regionId={RegionId}", regionId);
            return StatusCode(500, ApiResponse<object>.Fail(
                $"Region sync for '{regionId}' failed.", new[] { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/static-hotels/stuba/sync/hotel/{hotelId}
    ///
    /// Force-refreshes a single hotel's static profile from STUBA,
    /// bypassing hash comparison (always writes).
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
            _logger.LogInformation(
                "Single hotel STUBA static sync initiated for hotelId={HotelId}", hotelId);

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
                $"Hotel '{hotelId}' not found. Run a sync first."));

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
    /// Returns the raw index file: last full sync time, total count,
    /// and all hotel metadata entries (hash, filePath, lastUpdated).
    ///
    /// NOTE: For large datasets (&gt;100K hotels) this can be a big payload.
    /// Prefer /hotels?page=N for paginated access.
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
