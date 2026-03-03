# US-2.2: Check Booking Status

## User Story
As a vendor integrator,
I want to check booking status,
So that I can show current booking state to my users.

## Acceptance Criteria
1. Endpoint `check-booking` returns normalized status values.
2. Response includes booking reference and status reason when available.

## Notes
- Target endpoint: `POST /api/hotels/check-booking`.
