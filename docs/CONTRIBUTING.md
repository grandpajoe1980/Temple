# Contributing Guide

## Workflow
1. Create a feature branch from `main`.
2. Implement changes + update any impacted docs (see Documentation Update Policy in docs/README.md).
3. Run build & basic tests locally (scripts to be added).
4. Open PR referencing roadmap item or decision #.
5. Ensure checklist below is complete.

## PR Checklist
- [ ] Code builds (`dotnet build` succeeds)
- [ ] New/changed endpoints documented in ROUTING.md
- [ ] Data model changes documented in DATA_MODELS.md
- [ ] Decisions updated (DECISIONS.md) if direction changed
- [ ] Security implications considered (auth, tenant isolation)
- [ ] Added/updated tests (once test project exists)
- [ ] No secrets committed

## Commit Message Style
Conventional commits recommended:
- feat(auth): add refresh token rotation
- fix(schedule): correct utc conversion bug
- docs(roles): expand capability examples

## Branch Naming
`feat/<area>-<short-desc>` | `fix/<area>-<issue>` | `chore/<tooling>`

## Coding Standards
- C# nullable enabled; no `#nullable disable` unless justified.
- Prefer records for immutable value objects (future additions).
- Keep controllers / minimal endpoints thin; push logic into Application layer.

## Documentation Discipline
If it isn't documented, it isn't done. Reject PRs lacking doc updates for externally visible changes.

## Security Reports
Report vulnerabilities privately (contact to be added) instead of filing public issue.
