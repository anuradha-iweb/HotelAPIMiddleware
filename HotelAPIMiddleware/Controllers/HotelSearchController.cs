using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelSearchController : ControllerBase
{
    private readonly HotelSearchAggregator _agg;
    private readonly HotelDetailsService _hotelDetailsService;

    public HotelSearchController(HotelSearchAggregator agg, HotelDetailsService hotelDetailsService)
    {
        _agg = agg;
        _hotelDetailsService = hotelDetailsService;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] UnifiedHotelSearchRequest request, CancellationToken ct)
    {
        var result = await _agg.SearchAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("get_hotel_details")]
    public async Task<IActionResult> GetHotelDetails([FromBody] GetHotelDetailsRequest request, CancellationToken ct)
    {
        var details = await _hotelDetailsService.GetHotelDetailsAsync(request, ct);
        if (details is null)
            return NotFound(new { message = "No hotel details found for the given cacheId/searchId/hotelId." });

        return Ok(details);
    }
}
