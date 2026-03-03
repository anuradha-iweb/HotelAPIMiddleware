# Product Requirements Document (PRD)
## Product: Hotel API Middleware (B2B)
## Date: 2026-03-03
## Version: 1.0
## Language: English (simple and clear)

## 1. Product Overview
Hotel API Middleware is a B2B integration platform.
It connects with multiple hotel providers (currently STUBA and RateHawk), normalizes their data, and exposes one unified API for vendor systems.

### Core Value
- Vendors integrate once with our API instead of integrating with each provider.
- Provider complexity stays inside middleware.
- Vendor teams get consistent request/response structures.

## 2. Problem Statement
Without this middleware:
- Every vendor must handle multiple provider formats.
- Booking and cancellation flows differ by provider.
- Teams duplicate logic for mapping, error handling, and retries.
- Time to onboard vendors is high.

With this middleware:
- Search and booking lifecycle become standardized.
- Provider-specific differences are abstracted.
- Integrations are faster, safer, and easier to maintain.

## 3. Product Goals
1. Provide one stable B2B API contract across providers.
2. Support complete booking lifecycle for vendors.
3. Keep high reliability with graceful degradation.
4. Protect sensitive data with strict security controls.
5. Enable fast roadmap delivery with clear phased execution.

## 4. Scope
## 4.1 In Scope (Current State + Near-Term Roadmap)
1. Unified hotel search.
2. Hotel details retrieval from cached search context.
3. Static hotel content sync (STUBA).
4. Booking lifecycle APIs:
   - `POST /api/hotels/confirm-booking`
   - `POST /api/hotels/check-booking`
   - `POST /api/hotels/cancel-booking`
5. Provider-agnostic error model and status model.
6. Security model including PII handling, audit logging, and secrets policy.
7. Epics and user stories for delivery planning.

## 4.2 Out of Scope (for this PRD cycle)
1. UI applications.
2. Payment gateway orchestration.
3. BI dashboards.
4. Long-term multi-year strategy.
5. Story point or time estimation.

## 5. Stakeholders and Users
## 5.1 Stakeholders
- Product owner
- Engineering team
- QA team
- DevOps and Security team
- Vendor onboarding team

## 5.2 Primary Users
- Vendor technical teams integrating hotel search and booking APIs.
- Internal operations/support teams tracking booking states.

## 6. Current-State Product Baseline (As-Is)
Based on current implementation:
1. Search endpoint exists: `POST /api/hotels/search`.
2. Hotel details endpoint exists: `POST /api/hotels/get_hotel_details`.
3. STUBA static sync endpoint exists: `POST /stuba/static-data/sync`.
4. Booking prepare business logic exists in service layer, but full booking lifecycle endpoints are not yet exposed.
5. Search response is cached for downstream details flow.
6. Provider isolation behavior exists (partial success pattern).

## 7. Target-State Product Definition (Near-Term)
The middleware should provide a clear booking lifecycle in addition to search and details:
1. Search hotels.
2. View hotel details.
3. Confirm booking.
4. Check booking status.
5. Cancel booking.

Each lifecycle step should be provider-agnostic at API level, while provider-specific behavior remains internal.

## 8. Functional Requirements
## FR-1 Unified Search
1. System shall accept one unified search request for selected providers.
2. System shall run provider searches concurrently.
3. System shall return normalized hotels and provider execution metadata.
4. System shall return partial-success results when one provider fails.

## FR-2 Hotel Details
1. System shall return hotel details from cached search context.
2. System shall support provider-specific enrichment without changing common contract.
3. System shall return not found for invalid cache or hotel references.

## FR-3 Static Content Sync
1. System shall support STUBA static sync using configured sync sequence.
2. System shall store per-hotel static JSON files.
3. System shall return sync summary counts and step-level error visibility.

## FR-4 Confirm Booking
1. System shall expose `POST /api/hotels/confirm-booking`.
2. System shall validate booking payload and provider mapping requirements.
3. System shall call correct provider booking confirmation workflow.
4. System shall return normalized booking confirmation response.

## FR-5 Check Booking
1. System shall expose `POST /api/hotels/check-booking`.
2. System shall retrieve booking status from internal state and/or provider.
3. System shall return normalized status values (e.g., pending, confirmed, failed, cancelled).

## FR-6 Cancel Booking
1. System shall expose `POST /api/hotels/cancel-booking`.
2. System shall validate cancellation eligibility and rules.
3. System shall execute provider cancellation workflow.
4. System shall return normalized cancellation outcome.

## FR-7 Error Normalization
1. System shall return consistent error envelope format for all endpoints.
2. System shall include safe, actionable error messages without exposing secrets.
3. System shall separate validation errors, provider errors, and system errors.

## FR-8 Contract Compatibility
1. Existing vendor integrations shall not break due to non-versioned contract changes.
2. Any new response fields shall be additive by default.

## 9. Non-Functional Requirements
## NFR-1 Reliability
1. Provider failures shall be isolated.
2. One provider outage shall not fully stop service when other providers are healthy.

## NFR-2 Scalability
1. Service shall support concurrent provider calls and increasing vendor load.
2. Caching strategy shall reduce duplicate expensive provider calls.

## NFR-3 Security
1. No real provider secrets in source-controlled docs/examples.
2. Secrets must come from secure configuration sources.
3. PII must be protected in transit and in logs.

## NFR-4 Observability
1. Request tracing and correlation IDs shall be logged.
2. Provider call status and latency shall be tracked.
3. Booking lifecycle events shall be traceable end-to-end.

## NFR-5 Maintainability
1. Provider-specific logic shall remain modular.
2. API contracts shall remain stable and documented.
3. Testability shall improve for all lifecycle endpoints.

## 10. Security and Compliance Requirements
## 10.1 PII Handling
1. PII fields in booking requests/responses shall be classified.
2. PII shall be masked/redacted in application logs.
3. Only required PII shall be persisted, with minimum retention policy.

## 10.2 Audit Logging
1. All booking lifecycle actions (confirm/check/cancel) shall create audit events.
2. Audit event must include timestamp, actor/system, endpoint, booking reference, outcome.
3. Audit logs shall be immutable in storage policy.

## 10.3 Secrets Policy
1. API keys, auth headers, and passwords shall never be hardcoded in docs or code samples.
2. Secrets shall be loaded from environment variables or secret managers.
3. Any accidental secret exposure must trigger immediate rotation and incident process.

## 11. API Product Surface (Planned B2B Contract)
## Existing
1. `POST /api/hotels/search`
2. `POST /api/hotels/get_hotel_details`
3. `POST /stuba/static-data/sync`

## New Lifecycle Endpoints
1. `POST /api/hotels/confirm-booking`
2. `POST /api/hotels/check-booking`
3. `POST /api/hotels/cancel-booking`

## 12. Epics and User Stories
## Epic 1: Unified Search and Details
### User Story 1.1
As a vendor integrator, I want one search API for multiple providers so that I can avoid provider-specific integration.
- Acceptance criteria:
  1. Search works with one or many selected providers.
  2. Response includes provider execution summary.

### User Story 1.2
As a vendor integrator, I want hotel details from the search context so that I can show complete property information.
- Acceptance criteria:
  1. Details can be fetched by valid cache and hotel references.
  2. Invalid references return clear not-found response.

## Epic 2: Full Booking Lifecycle
### User Story 2.1 (Confirm)
As a vendor integrator, I want to confirm a booking through one endpoint so that booking is provider-agnostic for my team.
- Acceptance criteria:
  1. Endpoint `confirm-booking` is available and documented.
  2. Provider-specific logic is hidden behind unified response.

### User Story 2.2 (Check)
As a vendor integrator, I want to check booking status so that I can show current booking state to my users.
- Acceptance criteria:
  1. Endpoint `check-booking` returns normalized status values.
  2. Response includes booking reference and status reason when available.

### User Story 2.3 (Cancel)
As a vendor integrator, I want to cancel eligible bookings so that I can support post-booking changes.
- Acceptance criteria:
  1. Endpoint `cancel-booking` validates cancellation rules.
  2. Response indicates success/failure and effective cancellation state.

## Epic 3: Security and Audit Readiness
### User Story 3.1
As a security stakeholder, I want strict secrets handling so that credentials are never leaked.
- Acceptance criteria:
  1. No real secrets in repository docs/examples.
  2. Configuration docs use placeholders only.

### User Story 3.2
As an operations stakeholder, I want audit logs for booking actions so that incidents and disputes can be traced.
- Acceptance criteria:
  1. Confirm/check/cancel actions generate audit events.
  2. Events can be queried by booking reference and date range.

## Epic 4: Quality and Operability
### User Story 4.1
As QA, I want test coverage for lifecycle endpoints so that releases are stable.
- Acceptance criteria:
  1. Critical success and failure paths are tested.
  2. Regression checklist is enforced before release.

### User Story 4.2
As support, I want consistent error mapping and tracing so that troubleshooting is faster.
- Acceptance criteria:
  1. Error envelope is consistent across endpoints.
  2. Logs include correlation IDs and provider execution data.

## 13. Phase-Based Roadmap (Near-Term)
## Phase 1: Foundation Hardening
1. Finalize this PRD and sign-off.
2. Align contract patterns for lifecycle endpoints.
3. Implement security baseline:
   - PII masking rules
   - secrets externalization policy
   - audit event schema

## Phase 2: Booking Lifecycle API Delivery
1. Implement `confirm-booking` endpoint.
2. Implement `check-booking` endpoint.
3. Implement `cancel-booking` endpoint.
4. Add normalized response/error standards for all three.

## Phase 3: Quality and Operational Maturity
1. Add automated test coverage for lifecycle paths.
2. Add release readiness checklist enforcement.
3. Add observability dashboards and alerts for provider failures and booking failures.

## 14. Risks and Mitigations
1. Risk: Provider schema or behavior changes.
   - Mitigation: isolate mapping layer and add contract regression tests.
2. Risk: Lifecycle inconsistency across providers.
   - Mitigation: define strict normalized domain status model.
3. Risk: Sensitive data exposure.
   - Mitigation: PII redaction, secrets policy, and audit controls.
4. Risk: Increased latency with more providers.
   - Mitigation: concurrent execution, caching, timeout budgets.

## 15. Assumptions
1. This PRD is for B2B API consumers only.
2. Current focus is present-state plus near-term roadmap.
3. Functional requirements and story-based planning are preferred over effort estimation.
4. Performance targets remain indicative for now and can be finalized later.

## 16. Approval Checklist
1. Product confirms lifecycle scope (confirm/check/cancel) is correct.
2. Engineering confirms technical feasibility with current architecture.
3. Security confirms PII, audit, and secrets controls are adequate.
4. QA confirms user stories are testable and acceptance criteria are clear.
