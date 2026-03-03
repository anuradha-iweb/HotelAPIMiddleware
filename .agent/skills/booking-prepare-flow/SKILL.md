---
name: booking-prepare-flow
description: Provider-aware booking prepare/prebook workflow for HotelAPIMiddleware. Use when implementing or reviewing booking prepare logic, including Stuba quote-based prepare and RateHawk two-step prebook, request validation, normalized response mapping, and provider error handling. Trigger phrases include booking prepare, prebook, book_hash, and quoteId.
---

# Booking Prepare Flow

1. Read `references/provider-prepare-rules.md` before edits.
2. Apply deterministic sequence:
   - Identify provider from request.
   - Validate required fields for selected provider.
   - Execute provider-specific prepare/prebook flow.
   - Normalize into unified `BookingPrepareResponse`.
   - Verify consistent error mapping.
3. Never log credentials or full sensitive payloads.
4. Keep provider-specific complexity inside service/mappers.
5. Use `../../workflows/booking-delivery-plan.md` as execution checklist.
