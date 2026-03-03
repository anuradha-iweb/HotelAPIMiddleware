---
name: search-enrichment-architecture
description: Architecture workflow for unified hotel search enrichment in HotelAPIMiddleware using static profiles and external attributes. Use when implementing additive DTO enrichment, deterministic merge precedence, service-layer merge orchestration, compatibility strategy, and enrichment performance/error handling.
---

# Search Enrichment Architecture

1. Treat `docs/search-enrichment-architecture.md` as canonical design source.
2. Keep contract changes additive only unless explicit versioning is approved.
3. Enforce deterministic merge precedence and provider-neutral output defaults.
4. Keep merge orchestration in service layer, not controller/provider clients.
5. Include rollout, compatibility, caching, and error degradation checks.

Load reference: `references/enrichment-merge-rules.md`
