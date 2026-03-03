# US-3.2: Audit Logs for Booking Actions

## User Story
As an operations stakeholder,
I want audit logs for booking actions,
So that incidents and disputes can be traced.

## Acceptance Criteria
1. Confirm/check/cancel actions generate audit events.
2. Events can be queried by booking reference and date range.

## Notes
- Audit schema should include timestamp, action, endpoint, actor/system, booking reference, and outcome.
