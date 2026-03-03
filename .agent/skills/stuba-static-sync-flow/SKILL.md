---
name: stuba-static-sync-flow
description: STUBA static hotel data synchronization workflow for HotelAPIMiddleware. Use when implementing, reviewing, or operating /stuba/static-data/sync and related fetch/store logic, including countries, regions, region search, hotel details, and JSON file persistence.
---

# STUBA Static Sync Flow

1. Read `references/static-sync-sequence.md` before changes.
2. Preserve full sync sequence ordering and failure accounting.
3. Keep sync idempotent for unchanged hotel files.
4. Ensure summary output remains operationally useful.
5. Keep authority/credentials externalized and redacted in logs/docs.
