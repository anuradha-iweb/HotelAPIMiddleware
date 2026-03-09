using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;

namespace HotelAPIMiddleware.Contracts.Requests;

public class UnifiedBookingConfirmRequest
{
    public HotelProvider Provider { get; set; }
    public JsonElement Data { get; set; }
}
