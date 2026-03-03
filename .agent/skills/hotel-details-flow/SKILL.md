---
name: hotel-details-flow
description: Hotel details retrieval workflow based on cached search results in HotelAPIMiddleware. Use when implementing or reviewing /api/hotels/get_hotel_details behavior, cache lookup rules, selected-room marking, and optional RateHawk details enrichment.
---

# Hotel Details Flow

1. Read `references/details-cache-flow.md` first.
2. Keep details lookup dependent on cacheId/searchId/hotel identifiers.
3. Preserve null-not-found semantics for unresolved cache or hotel.
4. Keep RateHawk enrichment optional and failure-tolerant.
5. Validate selected room marking logic.
6. Use `../../templates/test-matrix-template.md` for scenario coverage.
