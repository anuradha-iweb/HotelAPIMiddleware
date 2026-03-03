# Epic 2: Full Booking Lifecycle

## Objective
Deliver complete booking lifecycle capabilities through unified API contracts.

## Business Value
- Enable vendor booking operations end-to-end.
- Hide provider-specific complexity behind consistent endpoints.

## Scope
- Confirm booking endpoint.
- Check booking status endpoint.
- Cancel booking endpoint.
- Normalized response and error patterns.

## API Targets
- `POST /api/hotels/confirm-booking`
- `POST /api/hotels/check-booking`
- `POST /api/hotels/cancel-booking`

## Related User Stories
- US-2.1 Confirm booking through one endpoint.
- US-2.2 Check normalized booking status.
- US-2.3 Cancel eligible bookings.

## Dependencies
- Provider-specific booking flows.
- Status normalization model.
- Error normalization standard.

## Success Criteria
1. All three lifecycle endpoints are available and documented.
2. Provider-specific workflows are abstracted behind unified contracts.
3. Booking outcomes are traceable and consistent.
