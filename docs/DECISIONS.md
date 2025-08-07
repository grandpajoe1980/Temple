# Decisions Log (Accepted)

| # | Topic | Decision | Rationale | Revisit |
|---|-------|----------|-----------|---------|
| 1 | Backend Framework | ASP.NET Core + SignalR | Performance, synergy with MAUI | After scale > 10k tenants |
| 2 | Auth Phase 1 | Self-managed ASP.NET Core Identity + JWT | Speed to MVP | When external compliance pressures increase |
| 3 | Multi-Tenancy | Single DB w/ TenantId discriminator | Simplicity, cost | At > 2k orgs high load -> evaluate sharding |
| 4 | Taxonomy Source | Curated internal JSON | Control, seed speed | After initial advisory feedback |
| 5 | Terminology Cache | Redis warm compiled dictionary | Fast lookup | If memory footprint grows large |
| 6 | Template Format | Versioned JSON (Git + DB ingest) | Diff friendly, auditable | If non-tech template authors needed |
| 7 | Chat | SignalR | Native integration | If needing > scale to multi-region fanout |
| 8 | Daily Content Source | Curated + optional AI enrichment moderated | Quality & safety | After community submissions open |
| 9 | Donations | Stripe first; plugin architecture for PayPal, Zelle, Venmo, Cash App, Bitcoin (BTCPay) | Fast launch + extensibility | After Stripe stable + demand signals |
|10 | Automation | Hangfire | Mature, dashboard | If complex event streaming emerges |
|11 | Presence | Redis pub/sub | Simplicity, scalable enough early | If cross-region latency issues |
|12 | Media Transcoding | Cloudflare Stream first | Offload complexity | If cost vs volume mismatch |
|13 | Web Deploy | Vercel (web) + AWS ECS (API) | Optimize DX & flexibility | Consolidate once infra stable |
|14 | IaC | Terraform | Ubiquitous, multi-cloud option | If team prefers Pulumi (TS) |
|15 | Analytics | Event table + Metabase | Low friction insights | When advanced ML/recs needed |
|16 | i18n | JSON (web) + .resx (MAUI) | Native patterns per stack | If central service warranted |
|17 | Role Model | Capability matrix + custom labels | Granular flexibility | If complexity > UX threshold |
|18 | Privacy | Build export/delete early | Compliance readiness | Continuous |
|19 | Search | Postgres FTS v1 | Zero extra infra | When relevance complexity rises |
|20 | MVP Slice | 3-phase vertical slices | Focus & velocity | After phase 1 retrospective |

## Donation Gateway Roadmap Detail
Phase 1: Stripe (one-time + recurring, test then live)
Phase 2: PayPal standard + Express (web) integration, abstract Provider interface.
Phase 3: Zelle & Venmo (linked instructions or API via PayPal; treat as asynchronous confirmation), Cash App manual code entry or webhook if available.
Phase 4: Bitcoin via BTCPay Server (self-host) or third-party processor; address volatility & accounting.

Provider abstraction interface (draft):
```csharp
public interface IDonationProvider {
    Task<InitiateDonationResult> InitiateAsync(DonationRequest request, CancellationToken ct);
    Task<WebhookResult> HandleWebhookAsync(HttpRequest request, CancellationToken ct);
    Task<RefundResult> RefundAsync(string providerDonationId, Money amount, CancellationToken ct);
}
```

## Automation Initial Triggers
- NewMemberJoined
- UpcomingEventReminder (T-24h)
- DonationReceived
- DailyContentRotation

## Next Actions
1. Create taxonomy seed JSON (religions, sects, default terminology & canonical texts placeholders).
2. Scaffold backend solution structure with projects (API, Domain, Infrastructure, Application, Shared Kernel).
3. Implement core entities (Tenant, User, TenantUserRole, Capability, ReligionTaxonomy, TerminologyOverride).
4. Add Redis + Postgres configuration placeholders.
5. Implement Stripe provider skeleton + provider interface.
6. Hangfire setup + recurring job registration stub (DailyContentRotation).

## Versioning
This file is append-only; changes recorded with date + reason below.

### Changelog
- 2025-08-07: Initial decisions snapshot created (all recommendations accepted; expanded donation gateways).
