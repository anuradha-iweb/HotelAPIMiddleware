# Provider Prepare Rules

## Service Reference
- `Services/BookingPrepareService.cs`

## Deterministic Flow
1. Determine provider enum from request.
2. Validate provider-required input fields.
3. Execute provider flow.
4. Map to unified response.
5. Return safe error response on failures.

## Stuba
- Single call pattern: `POST BookingPrepare`
- Input: `QuoteId` sourced from `RefNo`
- Mapper: `StubaBookingPrepareMapper`

## RateHawk
- Step 1: `POST search/hp/` using `hid` and stay/guest parameters.
- Step 2: `POST hotel/prebook/` using selected or discovered `book_hash`.
- Mapper: `RateHawkBookingPrepareMapper`

## Error Normalization
- Unknown provider -> unified error response.
- Invalid RateHawk `RefNo` (non-numeric hid) -> unified error response.
- HTTP failure in any provider step -> unified error response.
- Missing `book_hash` after Step 1 -> unified error response.

## Current Gap
- Controller endpoint for booking prepare is not currently exposed; tracked in backlog spec.
