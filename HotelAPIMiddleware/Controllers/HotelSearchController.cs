using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

[ApiController]
[Route("api/hotels")]
public class HotelSearchController : ControllerBase
{
    private readonly HotelSearchAggregator _agg;
    public HotelSearchController(HotelSearchAggregator agg)
    {
        _agg = agg;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] UnifiedHotelSearchRequest request, CancellationToken ct)
    {
        var result = await _agg.SearchAsync(request, ct);
        return Ok(result);
    }
}
