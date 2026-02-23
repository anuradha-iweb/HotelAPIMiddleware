using HotelAPIMiddleware.Common;
using HotelAPIMiddleware.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Models;
using HotelAPIMiddleware.StaticHotels.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

[ApiController]
[Route("stuba/static-data")]
[Produces("application/json")]
public sealed class StubaStaticDataSyncController : ControllerBase
{
    private readonly IHotelStaticDataFetchService _fetchService;
    private readonly ILogger<StubaStaticDataSyncController> _logger;

    public StubaStaticDataSyncController(
        IHotelStaticDataFetchService fetchService,
        ILogger<StubaStaticDataSyncController> logger)
    {
        _fetchService = fetchService;
        _logger = logger;
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<SyncSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Sync([FromBody] StubaStaticDataSyncRequest request, CancellationToken ct)
    {
        if (request.RegionId <= 0)
            return BadRequest(ApiResponse<object>.Fail("RegionId must be a positive integer."));

        if (string.IsNullOrWhiteSpace(request.Nationality))
            return BadRequest(ApiResponse<object>.Fail("Nationality is required (example: GB)."));

        if (request.Nights <= 0)
            return BadRequest(ApiResponse<object>.Fail("Nights must be at least 1."));

        var rooms = request.Rooms?.Any() == true
            ? request.Rooms
            : new List<StubaRoom> { new() { Adult = 2 } };

        try
        {
            var summary = await _fetchService.FetchByRegionAsync(
                request.RegionId,
                request.Nationality,
                request.Nights,
                rooms,
                request.ArrivalDate,
                ct);

            var message = $"Sync completed. JsonFilesCreated={summary.Created}, Updated={summary.Updated}, Skipped={summary.Skipped}, Failed={summary.Failed}.";
            if (summary.ApiStepErrors.Count > 0)
                message += $" API step errors: {summary.ApiStepErrors.Count}.";

            return Ok(ApiResponse<SyncSummary>.Ok(summary, message));
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest,
                ApiResponse<object>.Fail("Sync cancelled by client."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "stuba/static-data/sync failed for regionId={RegionId}", request.RegionId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("Sync failed due to an internal error.", new[] { ex.Message }));
        }
    }
}

public sealed class StubaStaticDataSyncRequest
{
    public int RegionId { get; set; }
    public string Nationality { get; set; } = "GB";
    public int Nights { get; set; } = 1;
    public DateOnly? ArrivalDate { get; set; }
    public List<StubaRoom>? Rooms { get; set; }
}
