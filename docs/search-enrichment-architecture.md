# Hotel Search Enrichment Architecture (Unified DTO)

## 1) Extended Common DTO Structure (Backward Compatible)

The existing API contract remains stable (`POST /api/hotels/search` and root object shape unchanged). We only **extend** `HotelResult` with optional enrichment nodes.

### Existing unchanged response envelope
- `searchId`
- `cacheId`
- `hotels[]`
- `providers[]`

### Proposed `HotelResult` extension
```csharp
public class HotelResult
{
    // Existing fields (unchanged)
    public string UniqueId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string RefNo { get; set; } = string.Empty;
    public string HotelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StarRating { get; set; }
    public Address Address { get; set; } = new();
    public List<RoomResult> Rooms { get; set; } = new();

    // New enrichment block (always present for structural consistency)
    public HotelContent Content { get; set; } = new();
}

public class HotelContent
{
    // Sourced from static hotel JSON
    public List<DescriptionItem> Descriptions { get; set; } = new();
    public List<ImageItem> Images { get; set; } = new();
    public List<string> Facilities { get; set; } = new();
    public ContactInfo Contact { get; set; } = new();
    public PolicyInfo Policies { get; set; } = new();
    public GeoPoint Geo { get; set; } = new();
    public HotelMeta Meta { get; set; } = new();

    // Sourced from new external API
    public HotelAttributes Attributes { get; set; } = new();
}

public class DescriptionItem
{
    public string Language { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ImageItem
{
    public string Url { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string PhotoType { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ContactInfo
{
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
}

public class PolicyInfo
{
    public string CheckInTime { get; set; } = string.Empty;
    public string CheckOutTime { get; set; } = string.Empty;
    public List<string> Languages { get; set; } = new();
}

public class GeoPoint
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class HotelMeta
{
    public bool StaticDataFound { get; set; }
    public DateTime? StaticDataLastSyncedUtc { get; set; }
}

public class HotelAttributes
{
    // New external API fields (example set, can evolve)
    public string ChainCode { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string PropertyCategory { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool? IsPreferred { get; set; }
}
```

### Compatibility notes
- Existing fields are untouched.
- New fields are additive only.
- Structural consistency rule is enforced by default object initializers.
- Unsupported provider fields are still present with empty values (`""`, `[]`, `null`).

---

## 2) Merge Strategy Design (Deterministic and Provider-Neutral)

For each `HotelResult`, produce final response using this precedence model:

1. **Base search result** (live availability/pricing/room data) 
2. **Static profile merge** (stable descriptive data)
3. **New external API properties merge** (attributes/metadata)

### Field-level precedence
- `Rooms`, `Rates`, availability-related fields: **search result wins**.
- `Name`, `StarRating`, `Address`: 
  - use search values if present;
  - fallback to static profile if search value missing/empty.
- `Content.*` block:
  - primarily from static profile;
  - then overlay with new API attributes where mapped.
- `Content.Attributes.*`:
  - from new API mapper only.
  - if unavailable for provider/hotel: keep empty defaults.

### Merge execution contract
Use a dedicated service in application layer:

```csharp
public interface IHotelContentMergeService
{
    Task EnrichAsync(List<HotelResult> hotels, CancellationToken ct);
}
```

Implementation responsibilities:
- Batch-resolve static profiles by `(provider, hotelId)`.
- Batch-resolve new API properties by normalized hotel key.
- Merge in-memory using deterministic rules.
- Never throw for missing static profile (graceful fallback).

---

## 3) Provider Mapper Strategy

## Stuba mapper
- Keep existing Stuba search mapping for rates/rooms.
- Populate base `HotelResult` normally.
- Do not directly load static/new API inside provider client.
- Stuba-specific IDs should support lookup key generation for enrichment (`Provider=STUBA`, `HotelId`).

## RateHawk mapper
- Map all currently available fields to common DTO.
- For fields absent in RateHawk domain:
  - Keep common nodes present.
  - Return defaults:
    - string => `""`
    - collection => `[]`
    - numeric optional => `null`
    - object => empty object with default children
- Never remove a field from `HotelResult` because one provider does not support it.

---

## 4) Updated Search Pipeline (Step-by-Step)

`POST /api/hotels/search`

1. Controller validates request and passes `CancellationToken` into service.
2. `HotelSearchAggregator` executes selected provider searches concurrently.
3. Each provider maps raw response to **common search DTO** (`HotelResult`, `RoomResult`, `RateResult`).
4. Aggregator builds unified response list.
5. `IHotelContentMergeService.EnrichAsync()` invoked in service layer.
6. Enricher loads static JSON profiles (cached, batch lookup).
7. Enricher loads new external API properties (cached, batch lookup).
8. Enricher merges static + new properties into `HotelResult.Content`.
9. Response metadata (`providers`, `cacheId`, `searchId`) finalized.
10. Return unified response to client.

Key placement constraints:
- Merge logic is **service layer only**.
- Controller stays thin.
- Provider client remains integration-only (no orchestration).

---

## 5) Sample Final Unified Response JSON

## (a) Stuba hotel (fully enriched)
```json
{
  "searchId": "b220c3b8-1a73-4ae2-95ba-0d84130ee910",
  "cacheId": "hotel_search_cache_b220c3b8_20260223120000",
  "hotels": [
    {
      "uniqueId": "hotel_4df2c18d-9827-4538-8f2c-2781243dce4a",
      "provider": "STUBA",
      "refNo": "STB-REF-93221",
      "hotelId": "98421",
      "name": "Marina Grand Hotel",
      "starRating": 5,
      "address": {
        "line1": "Beach Road 21",
        "line2": "Dubai Marina",
        "city": "Dubai",
        "state": "Dubai",
        "countryCode": "AE",
        "postalCode": "00000"
      },
      "rooms": [
        {
          "roomCode": "DLX",
          "roomName": "Deluxe Room",
          "rates": []
        }
      ],
      "content": {
        "descriptions": [
          {
            "language": "en",
            "type": "PropertyInformation",
            "text": "Luxury waterfront hotel in Dubai Marina."
          }
        ],
        "images": [
          {
            "url": "https://cdn.example.com/hotel/98421/main.jpg",
            "caption": "Exterior",
            "photoType": "Exterior",
            "width": 1920,
            "height": 1080
          }
        ],
        "facilities": ["Pool", "Gym", "WiFi"],
        "contact": {
          "phone": "+9714000111",
          "email": "info@marinagrand.example",
          "website": "https://marinagrand.example"
        },
        "policies": {
          "checkInTime": "14:00",
          "checkOutTime": "12:00",
          "languages": ["en", "ar"]
        },
        "geo": {
          "latitude": 25.0804,
          "longitude": 55.1403
        },
        "meta": {
          "staticDataFound": true,
          "staticDataLastSyncedUtc": "2026-02-20T16:20:44Z"
        },
        "attributes": {
          "chainCode": "MGH",
          "brandName": "Marina Collection",
          "propertyCategory": "Luxury",
          "tags": ["Beachfront", "FamilyFriendly"],
          "isPreferred": true
        }
      }
    }
  ],
  "providers": [
    { "provider": "STUBA", "success": true, "timeMs": 430, "error": null }
  ]
}
```

## (b) RateHawk hotel (unsupported fields returned as empty)
```json
{
  "searchId": "41f24963-7a53-4de2-a2d4-9fd9c7f3ef45",
  "cacheId": "hotel_search_cache_41f24963_20260223120000",
  "hotels": [
    {
      "uniqueId": "hotel_74c01967-1e1b-49cb-960e-571031e99b09",
      "provider": "RATEHAWK",
      "refNo": "RH-REF-4451",
      "hotelId": "RH-112299",
      "name": "City Central Inn",
      "starRating": 4,
      "address": {
        "line1": "",
        "line2": "",
        "city": "Istanbul",
        "state": "",
        "countryCode": "TR",
        "postalCode": ""
      },
      "rooms": [
        {
          "roomCode": "STD",
          "roomName": "Standard",
          "rates": []
        }
      ],
      "content": {
        "descriptions": [],
        "images": [],
        "facilities": [],
        "contact": {
          "phone": "",
          "email": "",
          "website": ""
        },
        "policies": {
          "checkInTime": "",
          "checkOutTime": "",
          "languages": []
        },
        "geo": {
          "latitude": null,
          "longitude": null
        },
        "meta": {
          "staticDataFound": false,
          "staticDataLastSyncedUtc": null
        },
        "attributes": {
          "chainCode": "",
          "brandName": "",
          "propertyCategory": "",
          "tags": [],
          "isPreferred": null
        }
      }
    }
  ],
  "providers": [
    { "provider": "RATEHAWK", "success": true, "timeMs": 510, "error": null }
  ]
}
```

---

## 6) Backward Compatibility Strategy

1. **Additive contract only**: Do not remove/rename existing fields.
2. **Stable endpoint**: keep `POST /api/hotels/search` unchanged.
3. **Consistent defaults**: new nodes always present with empty defaults.
4. **Version tolerance**:
   - clients using strict schemas can ignore unknown fields if configured;
   - for high-risk clients, support feature flag `EnableHotelContentEnrichment` (default ON in new environments, gradual rollout in existing).
5. **Schema governance**:
   - publish updated OpenAPI with examples for Stuba and RateHawk.
   - add consumer contract tests to ensure existing fields remain unchanged.

---

## 7) Clean Architecture Placement

### Controller layer
- Only receives request, passes to orchestrator service, returns unified response.
- No merge logic.

### Application/service layer
- `HotelSearchAggregator`: provider orchestration + response composition.
- `HotelContentMergeService`: enrichment orchestration (static + new API).
- `IHotelStaticProfileReader`, `IHotelAttributeProvider`: abstractions used by merger.

### Infrastructure layer
- Static JSON file store implementation.
- New API HTTP client implementation.
- Caching decorators (Memory/Distributed cache).

### Provider layer
- Raw provider request/response mapping only.
- No static JSON reads.
- No cross-provider normalization beyond common mapper.

---

## 8) Performance and Caching Strategy

1. **Avoid repeated static file I/O**
   - Cache `HotelStaticProfile` by key: `static:{provider}:{hotelId}`.
   - Suggested TTL: 6-24 hours (depends on sync frequency).
2. **Batch enrichment**
   - Collect all hotel keys from search result.
   - Fetch static profiles in parallel with bounded concurrency.
   - Fetch new API properties in bulk endpoint (if available) or in parallel throttled calls.
3. **Two-level cache (optional)**
   - L1: `IMemoryCache` for per-instance hot data.
   - L2: Redis for multi-instance consistency.
4. **Negative caching**
   - Cache missing static profile result briefly (e.g., 5 min) to avoid repeated disk lookups.
5. **Cancellation-aware async**
   - Pass `CancellationToken` through all async file/API operations.
6. **Partial enrichment budget**
   - Enrichment timeout budget (e.g., 300-500 ms); if exceeded, return base search data with empty content instead of failing entire response.

---

## 9) Error Handling Strategy

### Static JSON missing
- Do **not** fail search.
- Set:
  - `content.meta.staticDataFound = false`
  - all other `content` fields stay at defaults.
- Log at `Information` level with provider/hotel key.

### Static JSON malformed
- Do not fail search.
- Log warning with exception + hotel key.
- Skip static merge for that hotel.

### New API unavailable / timeout
- Do not fail search.
- Return default `content.attributes` values.
- Add provider execution diagnostic entry if needed (or enrichment diagnostics internally).

### Provider search failure
- Existing per-provider behavior remains (provider marked unsuccessful in `providers[]`).
- Successful providers still return enriched results where possible.

---

## Suggested Implementation Sequence

1. Extend common DTOs with `HotelContent` (additive).
2. Implement `IHotelContentMergeService` and unit tests for merge precedence.
3. Introduce static profile cache abstraction.
4. Integrate new API attribute client + mapper to `HotelAttributes`.
5. Wire enrichment in `HotelSearchAggregator` after provider mapping.
6. Add integration tests for:
   - Stuba with static + attributes.
   - RateHawk with empty defaults.
   - missing static profile.
7. Update OpenAPI examples and rollout via feature flag.
