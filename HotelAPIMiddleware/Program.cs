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

// ── Availability providers ─────────────────────────────────────────────────────

builder.Services.AddScoped<IHotelProvider, RateHawkHotelProvider>();
builder.Services.AddScoped<IHotelProvider, StubaHotelProvider>();
builder.Services.AddScoped<HotelSearchAggregator>();
builder.Services.AddScoped<BookingPrepareService>();

// ── Static hotel feature ───────────────────────────────────────────────────────

// StubaStaticClient reuses the "StubaClient" named HttpClient (same base URL + auth)
builder.Services.AddSingleton<IStubaStaticClient, StubaStaticClient>();

// File store is a singleton — holds no per-request state and manages its own semaphores
builder.Services.AddSingleton<IHotelStaticStore, HotelStaticFileStore>();

// Sync service is scoped (uses the singleton store + client)
builder.Services.AddScoped<IHotelStaticSyncService, HotelStaticSyncService>();

// ── Cache ─────────────────────────────────────────────────────────────────────

builder.Services.AddMemoryCache();

// ── App pipeline ──────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
