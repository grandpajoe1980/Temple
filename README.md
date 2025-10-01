---
title: Temple
status: pre-MVP
language: C# (.NET 8)
multiTenant: true
generated: 2025-08-07
---

# Temple

> Multi-tenant, religion-agnostic community & communications platform (work in progress).

## 1. Project Summary
Temple aims to provide any faith community (church, temple, synagogue, monastery, etc.) with a customizable digital hub: tenancy, terminology templating, schedules/events, chat, donations, media, automation (daily thought/prayer), and role/capability based access. The current repository contains an early vertical slice of the backend API (ASP.NET Core) plus an experimental .NET MAUI client prototype (`9.0/Apps/DeveloperBalance`).

## 2. Repository Structure
```
temple/
	README.md                <- This file
	docs/                    <- Architecture, roadmap, decisions, models, routing
	seed/                    <- Seed taxonomy / terminology JSON (planned expansion)
	src/Server/              <- .NET backend solution (modular monolith layout)
		Temple.sln
		Temple.Api/            <- Minimal API host (tenants + auth endpoints so far)
		Temple.Application/    <- Application layer (CQRS/services abstractions - WIP)
		Temple.Domain/         <- Domain entities & value objects (Tenants, Users, etc.)
		Temple.Infrastructure/ <- EF Core persistence, tenant services, migrations
		Temple.Tests/          <- Test project (initial slug tests)
	9.0/Apps/DeveloperBalance/ <- Prototype MAUI mobile app (unrelated demo / future client)
```

### Current Implemented Backend Entities
`Tenant`, `User`, `Lesson` (stub), `ScheduleEvent` (stub) with EF Core DbContext (`AppDbContext`).

## 3. High-Level Architecture (Condensed)
| Layer | Tech | Notes |
|-------|------|-------|
| Mobile | .NET MAUI | Shared iOS/Android UI (prototype present) |
| Web (future) | Next.js (React) | Admin portal & PWA client |
| API | ASP.NET Core Minimal APIs + (future) SignalR | Multi-tenant REST + realtime |
| Auth | ASP.NET Core Identity (planned) + JWT | Currently manual user + password hash |
| DB | PostgreSQL | Single-db multi-tenant (TenantId discriminator) |
| Cache (future) | Redis | Tenant slug + terminology cache, presence |
| Jobs (future) | Hangfire | Automation & scheduled rotations |
| Payments (future) | Stripe (phase 1) | Pluggable provider interface |
| Media (future) | S3 + Cloudflare Stream | Transcoding pipeline phase later |

More detail: `docs/ARCHITECTURE.md`.

## 4. Minimal API Surface (Current)
| Method | Path | Purpose |
|--------|------|---------|
| GET | /health | Liveness check |
| POST | /api/tenants | Create tenant (returns 201) |
| GET | /api/tenants/{id} | Fetch tenant by id |
| POST | /api/auth/register | Simplified user registration (auto-assigns first tenant) |
| POST | /api/auth/login | Returns JWT access token |
| GET | /api/users/me | Current user profile (requires auth) |

Planned endpoints & versioning: see `docs/ROUTING.md`.

## 5. Running the Backend (Local)
### 5.1 Prerequisites
- .NET 8 SDK (or later preview if required by solution)
- PostgreSQL 15+ running locally

### 5.2 Quick Start (Using Local PostgreSQL)
1. Ensure a Postgres database is available and note connection string (default expects `Host=localhost;Database=temple;Username=postgres;Password=postgres`).
2. From repository root:
```powershell
cd src/Server
dotnet build
dotnet run --project .\Temple.Api\Temple.Api.csproj
```
3. Navigate to Swagger UI (development only): http://localhost:5000/swagger
4. Health check: http://localhost:5000/health

### 5.3 Configuration
Configuration precedence: Environment Variables > `appsettings.Development.json` > defaults (hard-coded fallbacks – dev only).

| ENV / Key | Purpose | Default (dev) | Required For | Notes |
|-----------|---------|---------------|--------------|-------|
| ConnectionStrings__Postgres | EF Core connection | Host=localhost;Database=temple;Username=postgres;Password=postgres | API | Use strong password outside dev |
| Jwt__Secret | HMAC signing key | dev-secret-change | Auth | Must be 32+ chars in non-dev |
| Jwt__Issuer | Token issuer | temple.local | Auth | Match Audience for simple setups |
| Jwt__Audience | Token audience | temple.clients | Auth | Distinguish internal/public later |
| Jwt__ExpiryMinutes | Access token lifetime | 60 | Auth | Will shorten when refresh added |

Example PowerShell (session only):

```powershell
$env:ConnectionStrings__Postgres = "Host=localhost;Database=temple;Username=postgres;Password=postgres"
$env:Jwt__Secret = "replace-with-long-random"
```

Example `appsettings.Development.json` snippet:

```jsonc
{
	"ConnectionStrings": {
		"Postgres": "Host=localhost;Database=temple;Username=postgres;Password=postgres"
	},
	"Jwt": {
		"Secret": "dev-secret-change",
		"Issuer": "temple.local",
		"Audience": "temple.clients",
		"ExpiryMinutes": 60
	}
}
```

### 5.4 Basic Usage Flow
1. Create tenant: `POST /api/tenants { "name": "Community Name" }`
2. Register user: `POST /api/auth/register { "email": "user@example.com", "password": "Passw0rd!" }`
3. Login: `POST /api/auth/login { "email": "user@example.com", "password": "Passw0rd!" }` => receive `accessToken`.
4. (Future) Call protected endpoints with `Authorization: Bearer <token>`.

## 6. Data Model Snapshot (Current vs Planned)
Current tables (via EF Core): Tenants, Users, Lessons, ScheduleEvents.
Planned expansions: Donations, ChatChannels, Messages, AutomationRules, MediaAssets, AuditEvents (see `docs/DATA_MODELS.md`).

## 7. Security & Multi-Tenancy (Early Stage)
Present code does not yet enforce capability policies; all exposed endpoints are open or minimally validated. JWT includes `sub` (user id), `tid` (tenant id), `email`. Hardening tasks are tracked in roadmap milestones. Never deploy current state to production without additional controls (rate limiting, HTTPS enforcement, stronger secrets, password complexity rules, email verification).

## 8. Development Standards
See `docs/CONTRIBUTING.md` for workflow, conventional commits, and documentation update policy. Key rule: any contract change must update docs in the same PR.

## 9. Decision Log & History
Accepted architectural/product decisions live in `docs/DECISIONS.md` (append-only). Each entry includes rationale and revisit trigger. Pending or newly ratified decisions begin in `docs/PROJECT_DECISIONS.md` before promotion.

## 10. Roadmap (Condensed)
Milestone 1: Tenancy + Auth + Terminology seed + Basic schedule.
Milestone 2: Chat (general + announcements) + Donations (Stripe) + Notifications baseline.
Milestone 3: Automation engine + Daily content + Media upload (audio) + Recurrence & role matrix UI.
Further milestones: media transcoding, analytics, marketplace (see `docs/ROADMAP.md`).

## 11. Next Engineering Steps (Short List)
- [ ] EF Core migrations (baseline snapshot) & auto-apply dev
- [ ] Identity hardening (password policy, email confirmation)
- [ ] Tenant slug resolution middleware + header/subdomain strategy
- [ ] Redis integration (tenant slug + terminology cache proto)
- [ ] Stripe donation provider skeleton + provider interface contract
- [ ] Hangfire setup + DailyContentRotation recurring job stub
- [ ] Capability-based authorization policies wired to endpoints

- [ ] Integration + unit tests for tenant & auth flows (JWT issuance, slug collision)
- [ ] GitHub Actions (build, test, security scanning, formatting) CI pipeline
- [ ] Add OpenAPI document augmentation & client generation script

Progress Legend: ☐ not started | ◔ in progress | ✔ done (update as tasks advance).

## 12. Machine-Readable Metadata (Draft JSON)
```json
{
	"name": "Temple",
	"status": "pre-MVP",
	"language": "C# (.NET 8)",
	"framework": "ASP.NET Core Minimal APIs",
	"packages": {
		"EntityFrameworkCore": "9.0.0 (Api) / 8.0.8 (Infra Design)",
		"Npgsql": "9.0.0",
		"Serilog.AspNetCore": "8.0.1",
		"Identity": "2.2.0",
		"Jwt": "7.5.1",
		"Redis": "2.8.0"
	},
	"multiTenant": true,
	"auth": { "current": "custom minimal", "planned": "ASP.NET Core Identity + JWT + refresh" },
	"database": "PostgreSQL 16",
	"cache": "Redis 7",
	"roadmapMilestone": 1,
	"servicesCurrent": ["tenancy", "auth.basic"],
	"servicesPlanned": ["schedule", "chat", "donations", "automation", "media", "analytics"],
	"securityWarning": "Do not use in production yet",
	"next": ["ef-migrations", "stripe-provider", "capability-auth", "redis-cache", "hangfire-setup"]
}
```

## 13. Dependencies Snapshot

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 9.0.0 | ORM core (API) |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.0 | PostgreSQL provider |
| Microsoft.EntityFrameworkCore.Design | 9.0.0 / 8.0.8 | Tooling (migrations) |
| Serilog.AspNetCore | 8.0.1 | Structured logging |
| Microsoft.AspNetCore.Identity | 2.2.0 | Password hashing + identity primitives |
| System.IdentityModel.Tokens.Jwt | 7.5.1 | JWT creation/validation |
| StackExchange.Redis | 2.8.0 | Planned caching / presence |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger/OpenAPI UI |
| xunit (+ runner) | 2.5.3 | Testing |

Keep versions synchronized when upgrading EF Core major versions (Infrastructure + Api + Design).

## 14. License
TBD (no license file yet). All rights reserved until a license is added. Do not assume open-source reuse permissions.

## 15. Contribution
Open to early contributors—open PRs referencing roadmap or decision IDs. See `docs/CONTRIBUTING.md`.

---
Generated: 2025-08-07 (synchronize manually if substantial changes occur).
