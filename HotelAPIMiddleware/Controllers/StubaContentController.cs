using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

/// <summary>
/// Stuba Static Content sync endpoint.
///
/// POST /api/stuba-content/sync
///     Triggers a one-off sync of hotel content from the Stuba Static Content API.
///     Only changed or new hotel records are written to disk.
///     Returns a summary of the sync run.
///
/// GET  /api/stuba-content/hotels/{hotelId}
///     Returns the stored content record (raw Stuba JSON + metadata) for a single hotel.
/// </summary>
[ApiController]
[Route("api/stuba-content")]
public class StubaContentController : ControllerBase
{
    private readonly StubaContentSyncService _syncService;

    public StubaContentController(StubaContentSyncService syncService)
    {
        _syncService = syncService;
    }

    /// <summary>
    /// Trigger a Stuba static content sync.
    /// Provide <c>RegionIds</c> to override the default list from configuration.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromBody] StubaContentSyncRequest request,
        CancellationToken ct)
    {
        var result = await _syncService.SyncAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieve the stored content record for a single hotel.
    /// Returns 404 if the hotel has not been synced yet.
    /// </summary>
    [HttpGet("hotels/{hotelId:int}")]
    public async Task<IActionResult> GetHotel(int hotelId)
    {
        var doc = await _syncService.GetStoredHotelAsync(hotelId);
        if (doc is null) return NotFound(new { hotelId, error = "Hotel content not found in local storage." });

        // Return the root element so ASP.NET Core serializes it as plain JSON
        return Ok(doc.RootElement);
    }
}
