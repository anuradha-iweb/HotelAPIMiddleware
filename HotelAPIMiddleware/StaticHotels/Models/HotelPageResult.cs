namespace HotelAPIMiddleware.StaticHotels.Models;

/// <summary>
/// Paginated list of index entries, returned by GET /api/static-hotels/stuba/hotels.
/// </summary>
public class HotelPageResult
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public List<HotelIndexEntry> Items { get; set; } = new();
}
