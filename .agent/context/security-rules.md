# Security Rules

## Non-Negotiable Credential Policy
- Do not store real provider credentials in `.agent` content.
- Do not copy values from `appsettings.json` or `appsettings.Development.json` into docs, code snippets, prompts, tests, or logs.
- Replace sensitive values with placeholders, e.g. `<RATEHAWK_BASIC_AUTH>`.

## Redaction Standard
Redact these categories before sharing output:
- API keys
- Basic auth tokens
- Passwords
- Authority blocks
- Raw booking payloads containing guest identity data

## Environment Variable Naming Standard
- Local:
  - `HOTELAPI__PROVIDERS__RATEHAWK__BASEURL`
  - `HOTELAPI__PROVIDERS__RATEHAWK__BASICAUTH`
  - `HOTELAPI__PROVIDERS__STUBA__BASEURL`
  - `HOTELAPI__PROVIDERS__STUBA__AUTHAPIKEY`
- Dev/Prod follow same key path with environment-specific secret stores.

## Safe Logging Rules
Do:
- Log provider name, endpoint name, correlation/search/cache IDs, elapsed times, status codes.
- Log counts and coarse diagnostics.

Do not:
- Log full authorization headers.
- Log full request/response bodies from providers by default.
- Log personally identifying booking data.
- Log raw secret-bearing configuration objects.

## Review Gate
Any PR that touches provider config, auth wiring, or logging must include a security review note confirming this policy.
