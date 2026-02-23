using HotelAPIMiddleware.Providers.Stuba.Dto;
using HotelAPIMiddleware.StaticHotels.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

[ApiController]
[Route("stuba/static-data")]
public sealed class StubaStaticDataController : ControllerBase
{
    private readonly IHotelStaticDataFetchService _fetchService;

    public StubaStaticDataController(IHotelStaticDataFetchService fetchService)
    {
        _fetchService = fetchService;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        var arrivalDate = DateOnly.FromDateTime(DateTime.Today);

        var summary = await _fetchService.FetchAllCountriesAsync(
            nationality: "GB",
            nights: 1,
            rooms: new List<StubaRoom> { new() { Adult = 2 } },
            arrivalDate: arrivalDate,
            ct);

        return Ok(new
        {
            Message = "Stuba static data sync completed.",
            CountriesScope = "AllCountries",
            ArrivalDateUsed = arrivalDate.ToString("yyyy-MM-dd"),
            NationalityUsed = "GB",
            NightsUsed = 1,
            RoomsUsed = new[] { new { Adult = 2 } },
            JsonFilesCreated = summary.Created,
            JsonFilesUpdated = summary.Updated,
            JsonFilesSkipped = summary.Skipped,
            JsonFilesFailed = summary.Failed,
            HotelsDiscovered = summary.TotalDiscovered,
            FailedHotelIds = summary.FailedHotelIds,
            ApiStepErrors = summary.ApiStepErrors,
            Summary = summary
        });
    }
}
