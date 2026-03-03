# AGENTS.md - HotelAPIMiddleware

## Purpose
This `.agent` workspace defines repeatable AI workflows for search, hotel details, booking prepare, STUBA static sync, and enrichment architecture work in this repository.

## Operating Rules
- Keep runtime API behavior stable unless a task explicitly requests API change work.
- Apply additive contract changes only by default.
- Do not store real credentials in `.agent` files.
- Never copy secrets from `appsettings*.json` into documentation, prompts, logs, or examples.
- Prefer deterministic implementation checklists over free-form execution.

## Task Intake Format
Use this minimum task intake shape:
1. Goal
2. In scope
3. Out of scope
4. Acceptance criteria
5. Test matrix
6. Rollback plan

Use template: `templates/task-spec-template.md`

## Branch And Commit Convention
- Branch prefix by area:
  - `feature/search-*`
  - `feature/details-*`
  - `feature/booking-*`
  - `feature/static-sync-*`
  - `chore/agent-*`
- Commit message pattern:
  - `type(scope): summary`
  - Examples: `feat(booking): add booking prepare endpoint`, `chore(agent): update booking skill reference`

## Mandatory Pre-Merge Checks
1. Build succeeds: `dotnet build HotelAPIMiddleware.sln`
2. Smoke calls for changed endpoints succeed.
3. Contract impact reviewed and documented if DTO/controller changed.
4. Logging review confirms no sensitive payload/credential leakage.
5. Rollback steps are documented in the task spec.

## Link Map
- Context:
  - `context/project-map.md`
  - `context/domain-model.md`
  - `context/security-rules.md`
- Workflows:
  - `workflows/search-delivery-plan.md`
  - `workflows/booking-delivery-plan.md`
  - `workflows/release-checklist.md`
- Templates:
  - `templates/task-spec-template.md`
  - `templates/test-matrix-template.md`
  - `templates/api-change-template.md`
- Skills:
  - `skills/hotel-search-flow/SKILL.md`
  - `skills/hotel-details-flow/SKILL.md`
  - `skills/booking-prepare-flow/SKILL.md`
  - `skills/stuba-static-sync-flow/SKILL.md`
  - `skills/search-enrichment-architecture/SKILL.md`
- Backlog specs:
  - `backlog/booking-endpoint-spec.md`
  - `backlog/test-infrastructure-spec.md`
  - `backlog/config-hardening-spec.md`
