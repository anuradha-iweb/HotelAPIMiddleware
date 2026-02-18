using System.Text.Json;
using HotelAPIMiddleware.Common.Enums;
using HotelAPIMiddleware.Contracts.Requests;
using HotelAPIMiddleware.Contracts.Responses;
using HotelAPIMiddleware.Mappings;
using HotelAPIMiddleware.Providers.Interfaces;

namespace HotelAPIMiddleware.Services;

public class HotelSearchAggregator
{
    private readonly IEnumerable<IHotelProvider> _providers;

    public HotelSearchAggregator(IEnumerable<IHotelProvider> providers)
    {
        _providers = providers;
    }

    public async Task<HotelSearchResponse> SearchAsync(UnifiedHotelSearchRequest request, CancellationToken ct)
    {
        var response = new HotelSearchResponse
        {
            SearchId = Guid.NewGuid().ToString()
        };

        var selected = _providers
            .Where(p => request.Providers.Contains(p.Provider))
            .ToList();

        if (selected.Count == 0)
        {
            response.Providers.Add(new ProviderExecutionInfo
            {
                Provider = "NONE",
                Success = false,
                Error = "No providers selected."
            });
            return response;
        }

        var tasks = selected.Select(async p =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // ✅ Build provider request JSON for debugging (optional)
            string providerRequestJson = "";
            try
            {
                if (p.Provider == HotelProvider.RateHawk)
                {
                    var rhReq = ProviderRequestMappers.ToRateHawkRequest(request);
                    providerRequestJson = JsonSerializer.Serialize(rhReq, new JsonSerializerOptions { WriteIndented = true });
                }
                else if (p.Provider == HotelProvider.Stuba)
                {
                    var stReq = ProviderRequestMappers.ToStubaRequest(request);
                    providerRequestJson = JsonSerializer.Serialize(stReq, new JsonSerializerOptions { WriteIndented = true });
                }

                // Optional log
                if (!string.IsNullOrWhiteSpace(providerRequestJson))
                    Console.WriteLine($"{p.Provider} Request JSON:\n{providerRequestJson}");

                var result = await p.SearchAsync(request, ct);
                sw.Stop();

                var provName = p.Provider.ToString().ToUpperInvariant();

                // ✅ Ensure provider name in every hotel & rate
                foreach (var h in result.Hotels)
                {
                    h.Provider = provName;
                    foreach (var rm in h.Rooms)
                        foreach (var rate in rm.Rates)
                            rate.Provider = provName;
                }

                return new
                {
                    Info = new ProviderExecutionInfo
                    {
                        Provider = provName,
                        Success = result.Success,
                        TimeMs = (int)sw.ElapsedMilliseconds,
                        Error = result.ErrorMessage
                    },
                    Hotels = result.Hotels
                };
            }
            catch (Exception ex)
            {
                sw.Stop();

                return new
                {
                    Info = new ProviderExecutionInfo
                    {
                        Provider = p.Provider.ToString().ToUpperInvariant(),
                        Success = false,
                        TimeMs = (int)sw.ElapsedMilliseconds,
                        Error = ex.Message
                    },
                    Hotels = new List<HotelResult>()
                };
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);

        response.Providers = results.Select(x => x.Info).ToList();
        response.Hotels = results.SelectMany(x => x.Hotels).ToList();

        return response;
    }
}
