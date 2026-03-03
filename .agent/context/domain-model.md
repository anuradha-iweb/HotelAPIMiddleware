# Domain Model (Current)

## Search Domain
- Input: `UnifiedHotelSearchRequest`
- Output envelope: `HotelSearchResponse`
  - `searchId`
  - `cacheId`
  - `hotels[]`
  - `providers[]`
- Item model: `HotelResult` with nested `RoomResult` and `RateResult`.

## Details Domain
- Input: `GetHotelDetailsRequest`
- Lookup source: in-memory cached `HotelSearchResponse` by `cacheId`
- Output: selected `HotelResult` with optional RateHawk enrichment.

## Booking Prepare Domain
- Input: `BookingPrepareRequest`
  - `provider`, `refNo`, and RateHawk-specific fields (`checkIn`, `checkOut`, `guests`, `bookHash`, etc.)
- Output: `BookingPrepareResponse`
  - Unified shape for Stuba and RateHawk pre-commit step.
- Note: Service exists but controller endpoint is not exposed yet.

## Static Sync Domain
- Trigger: `POST /stuba/static-data/sync`
- Sequence: countries -> regions -> region search -> hotel detail fetch -> file persistence.

## Enrichment Architecture
- Canonical design document: `docs/search-enrichment-architecture.md`
- Principle: additive DTO extension and deterministic merge precedence.
