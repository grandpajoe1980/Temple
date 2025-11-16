import { TenantFeaturePermissions, TenantSettings } from "@prisma/client";
import { TenantRoleType } from "@/lib/constants";

type FeatureKey =
  | "enablePosts"
  | "enableCalendar"
  | "enableSermons"
  | "enablePodcasts"
  | "enableBooks"
  | "enableMemberDirectory"
  | "enableGroupChat"
  | "enableComments"
  | "enableReactions";

export function isFeatureEnabled(settings: TenantSettings | null | undefined, feature: FeatureKey) {
  if (!settings) return true;
  return Boolean(settings[feature]);
}

export function canRole(settings: TenantFeaturePermissions[], role: TenantRoleType, capability: keyof TenantFeaturePermissions) {
  const record = settings.find((entry) => entry.roleType === role);
  if (!record) return false;
  return Boolean(record[capability as keyof TenantFeaturePermissions]);
}

export function canManageTenant(role: TenantRoleType | undefined) {
  return role === "ADMIN";
}

export function canModerateContent(role: TenantRoleType | undefined, permissions: TenantFeaturePermissions[], fallbackAdmin = true) {
  if (!role) return false;
  if (fallbackAdmin && role === "ADMIN") return true;
  return canRole(permissions, role, "canModerateContent");
}

export function canCreatePosts(role: TenantRoleType | undefined, permissions: TenantFeaturePermissions[]) {
  if (!role) return false;
  if (role === "ADMIN") return true;
  return canRole(permissions, role, "canCreatePosts");
}

export function canCreateEvents(role: TenantRoleType | undefined, permissions: TenantFeaturePermissions[]) {
  if (!role) return false;
  if (role === "ADMIN") return true;
  return canRole(permissions, role, "canCreateEvents");
}

export function canInviteMembers(role: TenantRoleType | undefined, permissions: TenantFeaturePermissions[]) {
  if (!role) return false;
  if (role === "ADMIN") return true;
  return canRole(permissions, role, "canInviteMembers");
}

export function canApproveMembership(role: TenantRoleType | undefined, permissions: TenantFeaturePermissions[]) {
  if (!role) return false;
  if (role === "ADMIN") return true;
  return canRole(permissions, role, "canApproveMembership");
}
