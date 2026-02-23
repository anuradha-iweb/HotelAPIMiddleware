using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Providers.Interfaces;
using HotelAPIMiddleware.Providers.RateHawk;
using HotelAPIMiddleware.Providers.Stuba;
using HotelAPIMiddleware.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ProviderOptions>(builder.Configuration.GetSection("Providers"));

builder.Services.AddScoped<HotelSearchAggregator>();
builder.Services.AddScoped<BookingPrepareService>();

builder.Services.AddHttpClient("RateHawkClient", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
    http.BaseAddress = new Uri(opt.RateHawk.BaseUrl);

    http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", opt.RateHawk.BasicAuth);

})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli
});


builder.Services.AddHttpClient("StubaClient", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;

    http.BaseAddress = new Uri(opt.Stuba.BaseUrl);

    // Stuba auth
    http.DefaultRequestHeaders.TryAddWithoutValidation("AuthApiKey", opt.Stuba.AuthApiKey);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli
});


// Register providers normally
builder.Services.AddScoped<IHotelProvider, RateHawkHotelProvider>();
builder.Services.AddScoped<IHotelProvider, StubaHotelProvider>();

// Stuba Static Content API client – uses a separate base URL and body-based auth
builder.Services.AddHttpClient("StubaContentClient", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
    http.BaseAddress = new Uri(opt.StubaContent.BaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli
});

// Stuba content sync service
builder.Services.AddScoped<StubaContentSyncService>();

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
