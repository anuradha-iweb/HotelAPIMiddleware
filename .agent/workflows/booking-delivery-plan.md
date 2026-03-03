# Booking Delivery Plan

## Objective
Deliver booking process changes safely from spec to release without breaking search/details flows.

## Workflow
1. Draft task using `templates/task-spec-template.md`.
2. Confirm provider scope (Stuba, RateHawk, or both).
3. Identify affected layers:
   - controller
   - service
   - provider mapper/client
   - contracts
4. Define acceptance tests via `templates/test-matrix-template.md`.
5. Implement with deterministic provider flow checks.
6. Run pre-merge checks and release checklist.

## Provider-Specific Guardrails
- Stuba prepare path is single-step with `QuoteId`.
- RateHawk prepare path requires `/search/hp/` then `/hotel/prebook/`.
- Normalize error outputs into unified `BookingPrepareResponse` error shape.

## Done Criteria
- Endpoint behavior documented.
- Error mapping verified for invalid ref, missing hash, provider HTTP failures.
- No secret leakage in logs/examples.
