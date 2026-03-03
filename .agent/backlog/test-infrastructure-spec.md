# Backlog Spec: Test Infrastructure And Coverage Baseline

## Goal
Introduce automated tests and baseline coverage for service/controller critical paths.

## Proposed Work
1. Add test project(s) for unit/integration scope.
2. Add unit tests for:
   - `HotelSearchAggregator`
   - `HotelDetailsService`
   - `BookingPrepareService`
3. Add controller-level tests for search/details and future booking endpoint.
4. Add CI command for tests.

## Acceptance Criteria
- Tests run in one command.
- Critical happy/error paths covered.
- New changes require test matrix update.

## Rollback
- Remove test project references and CI test step.
