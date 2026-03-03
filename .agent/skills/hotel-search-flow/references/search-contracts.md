# Search Contracts And Flow

## Current Endpoint
- `POST /api/hotels/search` in `Controllers/HotelSearchController.cs`

## Aggregation Logic
- Implemented in `Services/HotelSearchAggregator.cs`
- Runs selected providers concurrently.
- Collects per-provider execution info and merged hotel list.
- Assigns `UniqueId` per hotel and caches response for details flow.

## Guardrails
- Keep `providers[]` diagnostics complete even with partial failures.
- Keep provider label normalized on hotel and rates.
- Avoid provider-specific response shape divergence in shared DTO.

## Validation Focus
- Empty provider selection behavior.
- Provider exception path behavior.
- Cache ID generation and TTL assumptions.
