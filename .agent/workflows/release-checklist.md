# Release Checklist

## Build And Runtime
- [ ] `dotnet build HotelAPIMiddleware.sln` passes.
- [ ] App starts and swagger loads.
- [ ] Changed endpoints return expected status and schema.

## Contract And Compatibility
- [ ] DTO changes are additive or explicitly versioned.
- [ ] Existing endpoint paths remain valid.
- [ ] Consumer-impact note attached for any contract updates.

## Security And Observability
- [ ] No secrets in source/docs/examples.
- [ ] Logging does not include credentials or sensitive payloads.
- [ ] Error paths include actionable but safe diagnostics.

## Verification
- [ ] Test matrix executed and recorded.
- [ ] Manual smoke script updated if endpoint behavior changed.
- [ ] Rollback steps documented and verified.
