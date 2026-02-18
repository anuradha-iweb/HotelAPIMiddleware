using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelAPIMiddleware.Controllers;

[ApiController]
[Route("api/hotels")]
public class BookingPrepareController : ControllerBase
{
    private readonly BookingPrepareService _service;

    public BookingPrepareController(BookingPrepareService service)
    {
        _service = service;
    }

    /// <summary>
    /// Booking prepare step. Accepts one hotel (Stuba or RateHawk) and returns
    /// a unified response with room details, cancellation policies, and
    /// (for RateHawk) the prebook hash required for the actual booking.
    ///
    /// Stuba   → single API call  : POST /BookingPrepare using QuoteId (refNo)
    /// RateHawk → two API calls   :
    ///   1. POST /search/hp/      – refresh rates for the hotel (hid = refNo)
    ///   2. POST /hotel/prebook/  – confirm price and obtain a p- prefixed hash
    /// </summary>
    [HttpPost("booking-prepare")]
    public async Task<IActionResult> BookingPrepare(
        [FromBody] BookingPrepareRequest request,
        CancellationToken ct)
    {
        var result = await _service.PrepareAsync(request, ct);
        return Ok(result);
    }
}
