using HotelAPIMiddleware.Infrastructure.Configuration;
using HotelAPIMiddleware.Providers.Interfaces;
using HotelAPIMiddleware.Providers.RateHawk;
using HotelAPIMiddleware.Providers.Stuba;
using HotelAPIMiddleware.Services;
using HotelAPIMiddleware.StaticHotels.Providers.Stuba;
using HotelAPIMiddleware.StaticHotels.Services;
using HotelAPIMiddleware.StaticHotels.Store;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Configuration ─────────────────────────────────────────────────────────────

builder.Services.Configure<ProviderOptions>(builder.Configuration.GetSection("Providers"));
builder.Services.Configure<StaticHotelOptions>(builder.Configuration.GetSection("StaticHotels"));

// ── Named HTTP clients ────────────────────────────────────────────────────────

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

// Availability API: testapijson.stuba.com (AuthApiKey header)
builder.Services.AddHttpClient("StubaClient", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
    http.BaseAddress = new Uri(opt.Stuba.BaseUrl);
    http.DefaultRequestHeaders.TryAddWithoutValidation("AuthApiKey", opt.Stuba.AuthApiKey);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli
});

// Static-content API: testcontent.stuba.com (auth is in request body, no header needed)
builder.Services.AddHttpClient("StubaContentClient", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<ProviderOptions>>().Value;
    http.BaseAddress = new Uri(opt.StubaContent.BaseUrl);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli
});

// ── Availability providers ─────────────────────────────────────────────────────

builder.Services.AddScoped<IHotelProvider, RateHawkHotelProvider>();
builder.Services.AddScoped<IHotelProvider, StubaHotelProvider>();
builder.Services.AddScoped<HotelSearchAggregator>();
builder.Services.AddScoped<BookingPrepareService>();

// ── Static hotel feature ───────────────────────────────────────────────────────

builder.Services.AddSingleton<IStubaStaticClient, StubaStaticClient>();
builder.Services.AddSingleton<IHotelStaticStore, HotelStaticFileStore>();
builder.Services.AddScoped<IHotelStaticSyncService, HotelStaticSyncService>();
builder.Services.AddScoped<IHotelStaticDataFetchService, HotelStaticDataFetchService>();

// ── Cache ─────────────────────────────────────────────────────────────────────

builder.Services.AddMemoryCache();

// ── App pipeline ──────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
