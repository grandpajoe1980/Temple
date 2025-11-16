export const TenantRoles = ["MEMBER", "STAFF", "CLERGY", "MODERATOR", "ADMIN"] as const;
export type TenantRoleType = (typeof TenantRoles)[number];

export const MembershipStatuses = ["REQUESTED", "APPROVED", "REJECTED", "BANNED"] as const;
export type MembershipStatus = (typeof MembershipStatuses)[number];

export const PostTypes = ["BLOG", "ANNOUNCEMENT", "BOOK"] as const;
export type PostType = (typeof PostTypes)[number];

export const ContentVisibilities = ["PUBLIC", "MEMBERS_ONLY"] as const;
export type ContentVisibility = (typeof ContentVisibilities)[number];

export const MediaTypes = ["SERMON_VIDEO", "PODCAST_AUDIO"] as const;
export type MediaType = (typeof MediaTypes)[number];

export const EventCategories = ["GENERAL", "SERVICE", "COMMUNITY", "STUDY"] as const;
export const EventVisibilities = ["PUBLIC", "MEMBERS_ONLY"] as const;
export const RSVPStatuses = ["GOING", "NOT_GOING", "INTERESTED"] as const;

export const ConversationRoles = ["PARTICIPANT", "MODERATOR"] as const;

export const NotificationTypes = [
  "NEW_DM",
  "NEW_TENANT_ANNOUNCEMENT",
  "NEW_EVENT",
  "EVENT_REMINDER",
  "COMMENT_ON_POST",
  "REPLY_TO_COMMENT"
] as const;

export const EntityTypes = ["POST", "EVENT", "MESSAGE", "TENANT"] as const;

export const MembershipApprovalModes = ["OPEN", "APPROVAL_REQUIRED"] as const;
