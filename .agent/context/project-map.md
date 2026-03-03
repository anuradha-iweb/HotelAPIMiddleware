# Project Map

## Runtime Entry
- `HotelAPIMiddleware/Program.cs`
  - Registers providers, services, static hotel services, memory cache, swagger, controllers.

## Controllers
- `HotelAPIMiddleware/Controllers/HotelSearchController.cs`
  - `POST /api/hotels/search`
  - `POST /api/hotels/get_hotel_details`
- `HotelAPIMiddleware/Controllers/StubaStaticDataController.cs`
  - `POST /stuba/static-data/sync`

## Service Layer
- `HotelAPIMiddleware/Services/HotelSearchAggregator.cs`
  - Runs selected providers concurrently and caches unified response.
- `HotelAPIMiddleware/Services/HotelDetailsService.cs`
  - Reads cached search, resolves hotel, optionally enriches RateHawk details.
- `HotelAPIMiddleware/Services/BookingPrepareService.cs`
  - Provider-specific booking prepare orchestration.

## Providers
- `HotelAPIMiddleware/Providers/Stuba/*`
- `HotelAPIMiddleware/Providers/RateHawk/*`
- Shared interface: `HotelAPIMiddleware/Providers/Interfaces/IHotelProvider.cs`

## Contracts
- Requests: `HotelAPIMiddleware/Contracts/Requests/*`
- Responses: `HotelAPIMiddleware/Contracts/Responses/*`

## Static Hotel Subsystem
- Sync and fetch services under `HotelAPIMiddleware/StaticHotels/*`
- Persisted static data under `HotelAPIMiddleware/HotelStaticData/stuba/*`
