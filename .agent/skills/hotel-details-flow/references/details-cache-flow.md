# Details Cache Flow

## Current Endpoint
- `POST /api/hotels/get_hotel_details`

## Current Service Logic
- `Services/HotelDetailsService.cs`
- Reads `HotelSearchResponse` from in-memory cache by cacheId.
- Optionally validates incoming searchId.
- Resolves hotel by uniqueId/hotelId/id.
- Optionally calls RateHawk hotel page endpoint for enrichment.
- Marks selected room using roomId.

## Guardrails
- Missing cache/hotel returns null -> controller returns not found.
- Enrichment failure should not break base hotel details response.
- Keep cloned output semantics to avoid mutating cached object.
