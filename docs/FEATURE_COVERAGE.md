<!-- Backlog & Completion Tracking inserted -->
## Active Development Backlog (Priority Order)
The following items remain (statuses were ✅/🟡/🔴/⏩ in the table below). Focus moves top→down; when an item (or sub-bullets) is finished, move it to the Completed list and promote the next.

1. Security Hardening & Core Platform
   - Stripe: real SDK integration + webhook signature verification (replace placeholders)
   - Protect Hangfire dashboard (auth/policy) & enable persistent storage
   - Harden rate limiting: fine-grained per capability / route groups (tenant/IP already in place)
   - Consistent donation provider field naming (ProviderDonationId vs ProviderPaymentId)
2. Email Infrastructure
   - SMTP provider wiring using new global settings (smtp.host, port, user, secure, from)
   - Outbound email service abstraction + background queue
   - Verification & password reset email dispatch (replace manual token polling)
3. Authorization & Roles
   - Custom roles CRUD (persist capability sets)
   - Capability hash dynamic regeneration & invalidation broadcasting
4. Automation & Rules Engine
   - General rule evaluator (ConditionJson -> predicate, ActionJson -> operations)
   - Event-driven triggers (DonationReceived, NewMemberJoined, ScheduleEventSoon)
   - Extend lesson rotation with rule-based filtering & audit events
5. Notifications System
   - Preference model endpoints (channel + category granularity)
   - Delivery adapters (email placeholder first; future push/SMS)
   - Dispatch/background job w/ retry & status updates
6. Media Pipeline
   - Real object storage integration (e.g., S3 / Azure Blob) + presigned upload
   - Basic image processing (thumbnail) & secure asset serving
   - Placeholder for audio streaming (progressive/mp3) groundwork
7. Search Enhancements
   - Include lessons + descriptions in search index
   - Postgres FTS indices & ranked multi-entity results
   - Pagination & scoring normalization
8. Scheduling Improvements
   - Implement recurrence rule expansion & storage logic
   - Event categories / types taxonomy & filtering
   - Reminder dispatch integration with notifications
9. Donations & Finance Extensions
   - Robust Stripe recurring reconciliation & status sync
   - Donation statements export
   - Refund management endpoints + audit
10. Privacy & Compliance
    - User data export (JSON bundle) endpoint
    - User account deletion / soft-delete flow w/ grace period
11. Internationalization (i18n)
    - Resource files + culture negotiation
    - Surface terminology overrides in API responses
12. Chat Evolution
    - Presence pruning & Redis pub/sub scaling
    - Threading & reactions models + endpoints
    - Announcement channel enforcement & moderation audit
13. Content & Daily Rotation
    - Finish daily content rotation algorithm (taxonomy-weighted, no repeats span)
    - Content tagging search inclusion & simple relevance boost
14. Analytics & Insights
    - Dedicated analytics event model + ingestion helpers
    - Basic dashboard endpoints (donations, engagement, content usage)
15. International Marketplace (Deferred)
    - Template marketplace architecture skeleton (scaffold only)
16. Misc Hardening & Polish
    - Audit coverage expansion (sensitive endpoints)
    - Index review & migrations for hot queries (events, lessons search)
    - Tenant taxonomy post-create change API

## Planned Church-Life Feature Set (Expanded Scope)
The following domain features will be incrementally implemented (Advanced AI / ML features deferred):

1. People & Membership
   - Member profiles (household/family linking) (IN PROGRESS)
   - Assimilation pipeline (guest→attender→member)
   - Attendance tracking (services, classes)
   - Pastoral care notes (role-scoped confidentiality)
   - Prayer requests (confidentiality levels, answered tracking)
   - Milestones (baptism, membership, dedication)
2. Groups & Discipleship
   - Group directory & enrollment
   - Meeting scheduling & attendance
   - Curriculum distribution
   - Discipleship pathway tracking
3. Worship & Services
   - Service plans / orders of service
   - Song library (keys, arrangements)
   - Set list versioning
4. Volunteers & Scheduling
   - Positions & qualifications
   - Availability & assignment workflow
   - Background check status tracking
5. Children & Youth
   - Secure check-in/out (labels, guardians)
   - Room capacity & rosters
   - Incident reports
6. Giving & Stewardship
   - Pledges / campaigns & progress (IN PROGRESS - basic campaign create/list, pledges, raised tracking via fund ledger entries)
   - Designated funds ledger (IN PROGRESS - funds CRUD, ledger entries, balance denormalized)
   - Non‑cash gift intake (IN PROGRESS - gift create/list)
7. Finance & Operations
   - Simple budget vs actual categories (IN PROGRESS - finance goals added)
   - Expense submission & approval (IN PROGRESS - submit, list, approve/reject implemented)
   - Facility/resource booking
8. Events & Registrations
   - Public registrations (capacity, waitlist)
   - Ticketing / QR entry
9. Communication & Engagement
   - Bulk email/SMS lists & segmentation
   - Announcement scheduling
   - Feedback / survey forms
10. Outreach & Missions
   - Trip team management & fundraising progress
   - Benevolence request workflow
11. Care & Counseling
   - Appointment scheduling
   - Confidential case records & follow-up reminders
12. Media & Content Enhancements
   - Sermon series metadata & scripture references
   - Transcription & searchable archive (DEFER AI)
13. Workflow & Automation
   - Visual workflow builder (forms/attendance/giving triggers)
   - Task assignments & SLA reminders
14. Forms & Data Capture
   - Form builder with conditional logic
   - Routing to workflows
15. Security & Compliance
   - Extended audit coverage (care notes access, exports)
   - Background check lifecycle
   - Consent tracking & data retention policies
16. Analytics & Insights
   - Attendance / giving / engagement dashboards
   - Cohort retention metrics
17. Multi-Campus / Multi-Site
   - Campus entity & roll-up reporting
18. Internationalization & Accessibility
   - Resource localization & RTL support
   - Accessibility compliance improvements
19. Integrations (Foundational)
   - Calendar feeds (iCal)
   - Streaming / media provider hooks
   - Accounting export
20. Advanced AI (Deferred)
   - AI recommendation & sentiment (DEFERRED)
   - Automated follow-up drafting (DEFERRED)

## Recently Completed / Baseline Implemented
Items below were part of earlier backlog now functionally present (further hardening may still appear above):
 - Tenant resolution & creation
 - Core authentication (register/login/refresh) with grace-period email verification
 - Guest account creation & upgrade flow
 - Basic password reset & verification token flows
 - Capability-based authorization & static role matrix
 - Lesson CRUD + publish & initial automation rotation state
 - Daily content entity + rotation job scaffold
 - Media asset models & attachment linking (storage pending)
 - Donations basic create + leaderboard
 - Chat hub with channels, presence (basic), typing indicators
 - Terminology override middleware & endpoints
 - Rate limiting (tenant-aware global + targeted auth/donation policies)
 - Lesson automation state + manual override endpoints
 - Super admin global settings (universal configuration store)

# Feature Coverage Audit (2025-08-07)

This document maps the documented feature set (ROADMAP, ARCHITECTURE, ROUTING, ROLES_PERMISSIONS, DATA_MODELS) to the current implementation in the repository and highlights gaps / partial work / risks.

## legend
- ✅ Implemented (basic)  
- 🟡 Partial / stub / MVP slice only  
- 🔴 Missing  
- ⏩ Deferred (explicitly future per roadmap)  
- ⚠ Risk / needs hardening

## foundation / cross-cutting
| Feature | Status | Notes |
|---------|--------|-------|
| ASP.NET Core API skeleton | ✅ | Minimal APIs present in `Program.cs` |
| Serilog logging | ✅ | Config-driven; ensure configuration file has sinks (not audited here) |
| Correlation / Trace Id middleware | ✅ | `UseCorrelationId()` present (implementation not reviewed) |
| Exception handling middleware | ✅ | `UseGlobalExceptionHandling()` present (implementation not reviewed) |
| Security headers | ✅ | Middleware present; CSP static baseline |
| Rate limiting | 🟡 | Added tenant-aware global + auth/donation specific policies; still lacks per-capability granularity |
| Tenant resolution | ✅ | Subdomain + `X-Tenant-Slug` header; no query param fallback (docs mention dev fallback) |
| Authentication (email/password) | ✅ | Register, login, refresh, verification, password reset endpoints implemented |
| Email verification flow | 🟡 | Tokens generated; no outbound email dispatch integration |
| Password reset flow | 🟡 | Token generation works; no email service integration |
| Authorization (capabilities) | ✅ | Static capability list + role mapping; policy registration automatic |
| Capability hash invalidation | 🟡 | Hash provider static; no dynamic recompute / RoleVersion update path yet |
| Audit logging | 🟡 | Writer used in selected auth & event creation flows; coverage not comprehensive |
| Observability (metrics/tracing) | 🔴 | No OpenTelemetry instrumentation present |
| Caching (Redis) | 🟡 | Redis multiplexer registered (best-effort); no usage yet for terminology or tenant slug caching |
| Terminology resolution | ✅ | Middleware resolves & stores dictionary per request; no Redis/invalidations yet |
| Multi-tenancy data scoping | ✅ | Entities include `TenantId`; queries mostly scoped (spot-check recommended) |

## identity & roles
| Item | Status | Notes |
|------|--------|-------|
| Base roles enum & capability matrix | ✅ | `RoleCapabilities` & `Capability` classes; subset only |
| Custom role labels | ✅ | Endpoint to set label per user; not full custom roles (no new capability sets) |
| Custom roles (new capability sets) | 🔴 | Data model placeholder `RoleVersion` only; no CRUD or persistence of custom role definitions |
| Role versioning & hash invalidation | 🟡 | `RoleVersion` entity; not integrated with capability changes |

## users & profiles
| Feature | Status | Notes |
|---------|--------|-------|
| Current user profile endpoint | ✅ | `/api/v1/users/me` |
| Profile update | ✅ | `POST /api/v1/profile` limited to display name |
| Data export / delete (privacy) | 🔴 | Not implemented |

## tenancy & taxonomy
| Feature | Status | Notes |
|---------|--------|-------|
| Tenant create & fetch | ✅ | `/api/v1/tenants` and slug/id routes |
| Taxonomy seed ingest | ✅ | JSON ingestion logic on startup |
| Tenant selects taxonomy | 🟡 | Creation supports religion/sect selection + taxonomy endpoints; still no post-create change API |
| Template / terminology override | ✅ | Terminology override endpoint present |
| Template layered override resolution | 🟡 | Basic endpoints expose hierarchy; dynamic layered merge still pending |

## scheduling
| Feature | Status | Notes |
|---------|--------|-------|
| Create event | ✅ | `/schedule/events` |
| List events (pagination) | ✅ | Basic paging implemented |
| Recurrence rules | 🔴 | Field placeholder only; no logic |
| Categories / types beyond basic | 🔴 | Only simple `Type` string |
| Reminders / notifications | 🔴 | Not implemented |

## chat & realtime
| Feature | Status | Notes |
|---------|--------|-------|
| SignalR hub | ✅ | `ChatHub` with send + group join logic |
| Channel list endpoint | ✅ | `/chat/channels` |
| Post & list messages (REST) | ✅ | Implemented with capability gating |
| Presence / typing indicators | � | Basic in-DB presence + typing event; lacks Redis scaling & pruning |
| Multi-channel custom creation | ✅ | Endpoint added with create/delete/join/leave; private membership rudimentary |
| Announcements permission separation | 🟡 | Capability constant exists; enforcement in hub only |
| Threading / reactions | 🔴 | Not implemented |

## content
| Feature | Status | Notes |
|---------|--------|-------|
| Lesson entity | ✅ | Data model only; no endpoints (creation, list, publish) |
| Daily content rotation | 🟡 | Job stub counts items; no rotation logic or taxonomy-specific selection strategies |
| Content tagging/search | 🟡 | Naive search hits messages & events only; lessons excluded |
| Media upload & assets | � | Asset create/list + upload URL stub, lesson/event attach endpoints; no real storage or processing |
| Sermon/audio streaming | 🔴 | Playback delivery not yet implemented (only asset linkage) |
| Video pipeline (future) | ⏩ | Future milestone |

## donations & finance
| Feature | Status | Notes |
|---------|--------|-------|
| Donation entity & basic creation | ✅ | Direct plus provider initiate endpoint |
| Stripe integration | 🟡 | Placeholder (no real Stripe SDK usage, no signature verification) |
| Webhook handling | 🟡 | Simplistic JSON; lacks security validation |
| Recurring donation flag | ✅ | Boolean stored; no schedule/reconciliation |
| Donation summary endpoint | ✅ | Sum & counts; lacks filtering, currency abstraction |
| Refund management | 🔴 | Not implemented |
| Statements & leaderboard | � | Donor leaderboard implemented; statements pending |

## automation & rules
| Feature | Status | Notes |
|---------|--------|-------|
| Automation rule CRUD (list/create) | ✅ | Basic endpoints; no update/delete or execution engine |
| Supported triggers | 🟡 | Data allows any string; only daily content job exists |
| Hangfire setup | 🟡 | Server running, but PostgreSQL storage commented out (in-memory default). Dashboard unauthenticated |
| Event-driven actions (DonationReceived etc.) | 🔴 | Not wired |
| Daily content rotation job | 🟡 | Placeholder logging only |

## search
| Feature | Status | Notes |
|---------|--------|-------|
| Naive search service | ✅ | Events + chat messages only |
| Postgres FTS integration | 🔴 | No FTS config / indices specifically for search scopes |
| Multi-entity ranking & pagination | 🔴 | Not implemented |

## notifications
| Feature | Status | Notes |
|---------|--------|-------|
| Notification entity & create endpoint | 🟡 | Records only; no delivery adapter(s) |
| Email / push / SMS dispatch | 🔴 | Not implemented |
| User preference model | 🔴 | Missing |

## media pipeline
| Feature | Status | Notes |
|---------|--------|-------|
| Media asset model & CRUD | 🔴 | Absent |
| Upload (pre-signed URL) | 🔴 | Absent |
| Transcoding integration (Cloudflare Stream) | 🔴 | Absent |

## analytics & insights
| Feature | Status | Notes |
|---------|--------|-------|
| Event table for analytics | 🟡 | Audit events exist; dedicated analytics events absent |
| Dashboard endpoints | 🔴 | Not implemented |
| Recommendation engine | 🔴 | Not implemented |

## marketplace / templates (future milestones)
| Feature | Status | Notes |
|---------|--------|-------|
| Template marketplace | ⏩ | Future milestone |
| Community submitted content moderation | ⏩ | Future |

## internationalization
| Feature | Status | Notes |
|---------|--------|-------|
| i18n resource structure | 🔴 | Not present yet (web & MAUI) |
| Terminology override integration in responses | 🟡 | Middleware resolves, but APIs do not surface substituted terms |

## security & compliance
| Feature | Status | Notes |
|---------|--------|-------|
| JWT access + refresh tokens | ✅ | Implemented; refresh revocation logic basic (no reuse detection) |
| Capability hash validation middleware | ✅ | Present; always passes unless mismatch (no dynamic updates) |
| Audit trail coverage | 🟡 | Partial (auth success/fail, event create, automation rule create, terminology override); other sensitive actions missing |
| Rate limiting per-tenant | 🔴 | Only IP limiter present |
| Stripe webhook signature verification | 🔴 | Missing |
| Data export / deletion | 🔴 | Not implemented |

## performance & scalability
| Feature | Status | Notes |
|---------|--------|-------|
| Pagination patterns | ✅ | Implemented on several endpoints |
| Index coverage | 🟡 | Several indices defined; need review for query patterns (e.g., events by StartUtc) |
| Background jobs persistence | 🟡 | Hangfire storage not enabled for Postgres |

## mobile (MAUI) project (DeveloperBalance sample)
The MAUI app in `9.0/Apps/DeveloperBalance` appears unrelated to Temple core feature set (likely example / seed). Not integrated with server APIs.

## high-priority gaps to address next (suggested sequence)
1. Security Hardening
   - Stripe webhook signature verification & real SDK integration
   - Rate limiting per tenant + route groups
   - Email delivery adapter (verification, password reset) + abstraction
2. Core Domain Completion
   - Lessons endpoints (CRUD + publish)
   - Custom roles CRUD & dynamic capability hash regeneration
   - Presence & channel management (chat.read capability, create channel endpoint)
3. Automation & Events
   - Implement trigger dispatcher (DonationReceived, NewMemberJoined)
   - Flesh out DailyContentRotation logic (rotation, caching)
4. Media Minimal Slice
   - Enhance storage: real provider integration, processing pipeline
5. Notifications
   - Preference model + queued dispatcher (Hangfire job)
6. Search
   - Include lessons & events description; add simple Postgres FTS indices
7. Privacy & Compliance
   - User data export + delete endpoints
8. Internationalization Baseline
   - Introduce i18n resource files & helper to surface resolved terms

## quick win hardening items
- Protect Hangfire dashboard with capability or role policy.
- Add query param tenant fallback for dev parity with docs (optional).
- Add swagger tags & version grouping.
- Add integration tests for auth, schedule, donations initiate, chat REST.

## data model discrepancies (docs vs code)
| Model | Docs Field Missing in Code | Code Field Missing in Docs | Notes |
|-------|----------------------------|-----------------------------|-------|
| TenantUser | CustomRoleLabel, CapabilitiesJson (planned) | N/A | TenantUser not yet reviewed in code (assumed present) |
| Donation | ProviderDonationId (docs), MetaJson | ProviderPaymentId, ProviderDataJson, Status, UpdatedUtc, Recurring flag naming | Align naming (ProviderDonationId vs ProviderPaymentId) |
| MediaAsset | Entire model | N/A | Needs creation |
| AutomationRule | CreatedUtc (code) | N/A | Acceptable additional audit field |
| AuditEvent | DataJson (docs) | Same | Matches |

## risks
- Security exposure via unauthenticated Hangfire dashboard.
- Inconsistent donation provider field names could complicate abstraction.
- Lack of webhook verification introduces spoof risk.
- Missing comprehensive authorization on all endpoints (spot-check needed).
- Terminology override not cached (perf) & could cause per-request DB hit.

## tracking
Consider creating GitHub issues for each 🟡/🔴 item with labels: area/security, area/chat, area/donations, etc.

---
Generated automatically; update alongside feature development to keep visibility high.
