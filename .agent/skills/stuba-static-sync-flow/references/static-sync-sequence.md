# Static Sync Sequence

## Trigger Endpoint
- `POST /stuba/static-data/sync`

## Sequence
1. `getAllCountries`
2. `getAllSearchRegionsByCountry`
3. `RegionSearch`
4. `getAllHotelsDetailsByHotelIds`

## Current Components
- Controller: `Controllers/StubaStaticDataController.cs`
- Fetch service: `StaticHotels/Services/HotelStaticDataFetchService.cs`
- Sync service: `StaticHotels/Services/HotelStaticSyncService.cs`
- Store: `StaticHotels/Store/HotelStaticFileStore.cs`

## Operational Guardrails
- Track created/updated/skipped/failed counts.
- Continue where possible on per-hotel failures.
- Keep summary and API step errors visible in response.
