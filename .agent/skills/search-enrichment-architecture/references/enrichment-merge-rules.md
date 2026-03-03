# Enrichment Merge Rules

## Canonical Doc
- `docs/search-enrichment-architecture.md`

## Non-Negotiable Rules
- Keep `POST /api/hotels/search` stable.
- Add fields; do not remove/rename existing ones.
- Keep unsupported provider fields present as defaults.
- Do not fail search because enrichment sources are missing/unavailable.

## Merge Precedence
1. Base provider search result (availability/rates)
2. Static profile data
3. External attribute overlays

## Placement
- Controller: thin
- Aggregation/enrichment orchestration: service layer
- Provider clients: integration only

## Rollout Checklist
- Feature-flag if needed.
- Add examples for enriched and non-enriched providers.
- Add tests for missing static profile and external API timeout fallback.
