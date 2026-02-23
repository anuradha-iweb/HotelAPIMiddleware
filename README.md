# HotelAPIMiddleware

## Static data sync endpoint

The STUBA static-data sync endpoint is available at:

- `POST /stuba/static-data/sync`

This endpoint orchestrates:

1. `getAllCountries`
2. `getAllSearchRegionsByCountry` (regions/cities)
3. `RegionSearch`
4. `getAllHotelsDetailsByHotelIds`

and saves one JSON file per hotel under the configured static storage path.
