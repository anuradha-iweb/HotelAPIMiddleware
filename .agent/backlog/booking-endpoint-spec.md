# Backlog Spec: Expose Booking Prepare Endpoint

## Goal
Expose booking prepare functionality via controller endpoint using existing `BookingPrepareService`.

## Current State
- `BookingPrepareService` exists.
- No booking controller/action currently exposes prepare endpoint.

## Proposed Change
- Add controller action under `api/hotels` (or dedicated booking controller) to accept `BookingPrepareRequest` and return `BookingPrepareResponse`.

## Acceptance Criteria
1. Endpoint reachable and documented in Swagger.
2. Stuba path works with quoteId via `RefNo`.
3. RateHawk path supports hp + prebook sequence.
4. Error responses are normalized and safe.

## Tests
- Valid Stuba request
- Invalid RateHawk refNo
- Missing RateHawk book_hash in HP response
- Provider HTTP failure paths

## Rollback
- Remove endpoint wiring and DI usage; keep service implementation intact.
