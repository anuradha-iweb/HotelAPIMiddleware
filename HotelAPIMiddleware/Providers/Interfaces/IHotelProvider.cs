using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;

namespace HotelAPIMiddleware.Providers.Interfaces;

public interface IHotelProvider
{
    HotelProvider Provider { get; }

    Task<ProviderSearchResult> SearchAsync(UnifiedHotelSearchRequest request, CancellationToken ct);
}
