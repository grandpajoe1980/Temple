# Documentation Index

| Doc | Purpose |
|-----|---------|
| ARCHITECTURE.md | System high-level architecture & components |
| DECISIONS.md | Accepted architectural & product decisions (append-only) |
| PROJECT_DECISIONS.md | Pending / recently answered decision prompts |
| ROADMAP.md | Milestones & delivery sequence |
| ROLES_PERMISSIONS.md | Roles, capabilities, authorization model |
| ROUTING.md | API surface & versioning strategy |
| MIDDLEWARE_PIPELINE.md | Backend middleware order & cross-cutting concerns |
| FRONTEND_PAGES.md | Planned pages/screens (web & mobile) |
| DATA_MODELS.md | Core entity definitions & relationships |
| CONTRIBUTING.md | How to contribute & doc update rules |

## Documentation Update Policy
1. Any code change impacting a documented contract (API route, entity schema, role capability, config) MUST update the relevant doc in the same commit.
2. If a decision changes direction, add an entry to DECISIONS.md with date & reason; never delete historical rows.
3. Introduce new docs via pull request referencing at least one roadmap item or decision.
4. Keep examples minimal & executable where possible.

## Open Gaps (To Be Authored Incrementally)
- Security hardening checklist.
- Deployment runbook.
- Backup & disaster recovery plan.
