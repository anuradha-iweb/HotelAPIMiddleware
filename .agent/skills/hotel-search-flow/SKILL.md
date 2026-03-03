---
name: hotel-search-flow
description: Unified hotel search workflow and contract-safe evolution for HotelAPIMiddleware. Use when implementing or reviewing /api/hotels/search behavior, provider aggregation, provider execution metadata, cache handling, and search response mapping across Stuba and RateHawk.
---

# Hotel Search Flow

1. Read `references/search-contracts.md` before editing search behavior.
2. Keep endpoint `POST /api/hotels/search` stable unless explicitly versioning.
3. Preserve provider isolation: one provider failure must not suppress successful providers.
4. Keep response contract additive and deterministic.
5. Validate cache ID generation and retrieval assumptions.
6. Run checklist in `../../workflows/search-delivery-plan.md`.
