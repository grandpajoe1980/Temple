# Core Data Models (Initial Draft)

## Tenant
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| Name | string | Display name |
| Slug | string | Unique URL identifier (lowercase kebab; auto-generated; max 80 chars) |
| Status | string | active/suspended/archived |
| CreatedUtc | DateTime | Audit |

## User (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| Email | string | Unique |
| PasswordHash | string | ASP.NET Identity style |
| DisplayName | string | Public name |
| CreatedUtc | DateTime | |

## TenantUser (Planned)
| Field | Type | Notes |
|-------|------|-------|
| TenantId | Guid | FK -> Tenant |
| UserId | Guid | FK -> User |
| RoleKey | string | System role key |
| CustomRoleLabel | string? | Tenant override |
| CapabilitiesJson | jsonb | Custom capability overrides |
| JoinedUtc | DateTime | |

## ReligionTaxonomy (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | string | Hierarchical id |
| ParentId | string? | Self-reference |
| Type | string | religion/sect |
| DisplayName | string | |
| CanonicalTexts | string[] | List |
| DefaultTerminologyJson | jsonb | Key-value map |

## Event (Schedule) (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK |
| Title | string | |
| StartUtc | DateTime | |
| EndUtc | DateTime | |
| RecurrenceRule | string? | RFC5545 later |
| Type | string | service/lesson/study |

## Donation (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | |
| UserId | Guid? | Anonymous allowed |
| Provider | string | stripe/paypal/etc |
| ProviderDonationId | string | Mapping |
| Amount | decimal | Minor units standardization later |
| Currency | string | ISO 4217 |
| CreatedUtc | DateTime | |
| MetaJson | jsonb | Raw provider payload subset |

## ChatChannel (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK |
| Name | string | |
| Type | string | general/announcement/private |
| Visibility | string | public/private |

## Message (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| ChannelId | Guid | FK |
| TenantId | Guid | FK |
| UserId | Guid | FK |
| Body | string | Text content |
| RichPayloadJson | jsonb | Attachments/embeds |
| CreatedUtc | DateTime | |

## AutomationRule (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK |
| TriggerType | string | enum |
| ConditionJson | jsonb | Filter logic |
| ActionJson | jsonb | Action definitions |
| Enabled | bool | |

## MediaAsset (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK |
| Type | string | audio/video/document |
| StorageKey | string | S3 path |
| Status | string | uploaded/transcoding/ready/failed |
| TranscodeProfile | string? | HLS1080 etc |
| CreatedUtc | DateTime | |

## AuditEvent (Planned)
| Field | Type | Notes |
|-------|------|-------|
| Id | Guid | PK |
| TenantId | Guid | FK |
| ActorUserId | Guid | FK |
| Action | string | e.g., ROLE_UPDATED |
| EntityType | string | tenant/user/event |
| EntityId | string | flexible |
| DataJson | jsonb | delta snapshot |
| CreatedUtc | DateTime | |

## Indexing & Constraints (Future)
- Unique index on Tenant.Slug.
- Composite index (TenantId, RoleKey) on TenantUser.
- GIN indexes on jsonb where needed (CapabilitiesJson).
