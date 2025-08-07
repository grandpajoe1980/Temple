# Pending / Required Product & Technical Decisions
For each question a recommended default ("Rec") is provided to keep momentum; confirm or adjust.

## 1. Primary Backend Framework
- Options: ASP.NET Core (C#), NestJS (Node), Django (Python)
- Criteria: Team skill, performance, realtime ease, ecosystem.
- Rec: ASP.NET Core + SignalR.
- Question: Confirm ASP.NET Core? (Y/N)
Y (Accepted)

## 2. Authentication Strategy Phase 1
- Options: Self-managed Identity, Auth0, Cognito, Clerk.
- Rec: Self-managed ASP.NET Core Identity + JWT to move fast; externalize later.
- Question: Accept? (Y/N)
Y (Accepted)

## 3. Multi-Tenancy Data Isolation
- Options: Single DB (TenantId discriminator), Schema per tenant, DB per tenant.
- Rec: Start single DB; enable migration path. Implement repository abstraction.
- Question: Agree? (Y/N)
Y (Accepted)

## 4. Religion & Sect Taxonomy Source
- Options: Curated internal JSON, External API, Community-contributed.
- Rec: Start curated internal JSON file + admin editor.
- Question: Confirm? (Y/N)
Y (Accepted)

## 5. Terminology Customization Delivery
- Options: Runtime DB lookups each request, In-memory cache per tenant, Distributed cache (Redis) warm on change.
- Rec: Layered: warm compiled dictionary stored in Redis with version key.
- Question: Approve? (Y/N)
Y (Accepted)

## 6. Template Format
- Options: JSON documents, Database rows, YAML.
- Rec: JSON versioned documents (Git-managed for audit) ingested to DB.
- Question: Approve? (Y/N)
Y (Accepted)

## 7. Chat Technology
- Options: SignalR, WebSockets custom, Third-party (PubNub / Ably), Matrix.
- Rec: SignalR initially (simplicity) then evaluate scaling.
- Question: Approve? (Y/N)
Y (Accepted)

## 8. Daily Prayer / Thought Content Source
- Options: Static curated pool, AI-generated with moderation, Community submissions with rating.
- Rec: Start curated + optional AI enrichment flagged for moderation.
- Question: Approve? (Y/N)
Y (Accepted)

## 9. Donation Processing
- Options: Stripe only, Stripe + PayPal, Multi-gateway plugin board.
- Rec: Stripe first; abstract provider interface. Roadmap to add PayPal, Zelle (bank transfer verification), Venmo (via PayPal APIs), Cash App (manual confirmation or API if available), and Bitcoin (BTC lightning / processor like BTCPay) as optional plugins. Terminology layer maps donation/tithe/offering per tenant template.
- Question: Approve? (Y/N)
Y (Accepted) — Multi-gateway expansion scheduled (see DECISIONS.md & ROADMAP update forthcoming).

## 10. Automation Engine Implementation
- Options: Hangfire (jobs), Quartz.NET, Custom event processor.
- Rec: Hangfire (dashboard + reliability).
- Question: Approve? (Y/N)
Y (Accepted)

## 11. Realtime Presence & Typing Indicators
- Options: In-memory (scale issues), Redis pub/sub, Dedicated service.
- Rec: Redis pub/sub channel model.
- Question: Approve? (Y/N)
Y (Accepted)

## 12. Media Transcoding
- Options: Self-host FFmpeg, AWS Elastic Transcoder/MediaConvert, Cloudflare Stream.
- Rec: Cloudflare Stream (offloads complexity) or self-host FFmpeg only after scale.
- Question: Choose: Cloudflare Stream (Y/N) else specify.
Y (Accepted for Cloudflare Stream)

## 13. Web Deployment Platform (Early)
- Options: Vercel, Azure App Service, AWS ECS/Fargate.
- Rec: Vercel for web front-end + AWS ECS for API.
- Question: Approve? (Y/N)
Y (Accepted)

## 14. Infrastructure as Code
- Options: Terraform, Pulumi, Bicep.
- Rec: Terraform.
- Question: Approve? (Y/N)
Y (Accepted)

## 15. Analytics Layer
- Options: Simple DB queries, Segment + Data Warehouse, Open-source pipeline.
- Rec: Start with event table + Metabase; evolve to warehouse.
- Question: Approve? (Y/N)
Y (Accepted)

## 16. Internationalization (i18n)
- Options: Resource files (.resx), JSON dictionaries, ICU-based service.
- Rec: JSON dictionaries for web + .resx for MAUI shared terms.
- Question: Approve? (Y/N)
Y (Accepted)

## 17. Role Capability Model
- Options: Fixed roles with flags, Capability matrix table, Policy scripts.
- Rec: Capability matrix + custom role labels mapping to sets.
- Question: Approve? (Y/N)
Y (Accepted)

## 18. Data Privacy Compliance Approach
- Options: Ad-hoc later, Build hooks early, External DPO service.
- Rec: Build export/delete endpoints early for user data.
- Question: Approve? (Y/N)
Y (Accepted)

## 19. Search Functionality
- Options: Postgres full-text, OpenSearch cluster, Algolia SaaS.
- Rec: Postgres FTS first; abstract indexing.
- Question: Approve? (Y/N)
Y (Accepted)

## 20. Initial MVP Timeline Split
- Milestone 1: Tenancy + Auth + Template selection.
- Milestone 2: Schedule + Chat + Donation basic.
- Milestone 3: Automation daily content + Custom terminology edit UI.
- Question: Accept? (Y/N)
Y (Accepted)

---
All recommendations accepted. Will codify into DECISIONS.md and update ROADMAP with multi-gateway donation expansion.
