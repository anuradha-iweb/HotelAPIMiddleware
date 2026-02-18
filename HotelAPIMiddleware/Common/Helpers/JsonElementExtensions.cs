using System.Globalization;
using System.Text.Json;

namespace HotelAPIMiddleware.Common.Helpers;

public static class JsonElementExtensions
{
    public static JsonElement? TryGet(this JsonElement el, string propName)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(propName, out var p) ? p : null;

    public static IEnumerable<JsonElement> EnumerateArraySafe(this JsonElement el)
        => el.ValueKind == JsonValueKind.Array ? el.EnumerateArray() : Enumerable.Empty<JsonElement>();

    public static IEnumerable<JsonElement> EnumerateArraySafe(this JsonElement? el)
        => el.HasValue && el.Value.ValueKind == JsonValueKind.Array ? el.Value.EnumerateArray() : Enumerable.Empty<JsonElement>();

    public static string? GetStringSafe(this JsonElement? el)
        => el.HasValue && el.Value.ValueKind == JsonValueKind.String ? el.Value.GetString() : null;

    public static int? GetIntSafe(this JsonElement? el)
    {
        if (!el.HasValue) return null;
        if (el.Value.ValueKind == JsonValueKind.Number && el.Value.TryGetInt32(out var i)) return i;
        if (el.Value.ValueKind == JsonValueKind.String && int.TryParse(el.Value.GetString(), out var s)) return s;
        return null;
    }

    public static decimal? GetDecimalSafe(this JsonElement? el)
    {
        if (!el.HasValue) return null;
        if (el.Value.ValueKind == JsonValueKind.Number && el.Value.TryGetDecimal(out var d)) return d;
        if (el.Value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(el.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s))
            return s;
        return null;
    }

    // ✅ Overloads for NON-nullable JsonElement (fixes LINQ .Select(x => x.GetStringSafe()))
    public static string? GetStringSafe(this JsonElement el)
        => ((JsonElement?)el).GetStringSafe();

    public static int? GetIntSafe(this JsonElement el)
        => ((JsonElement?)el).GetIntSafe();

    public static decimal? GetDecimalSafe(this JsonElement el)
        => ((JsonElement?)el).GetDecimalSafe();


}
