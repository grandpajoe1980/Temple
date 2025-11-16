"use server";

import { z } from "zod";
import { updateTenantSettings } from "@/services/tenantService";

const generalSchema = z.object({ tenantId: z.string(), name: z.string().min(2), creed: z.string().min(2), description: z.string().optional() });

export async function updateGeneralSettings(_: any, formData: FormData) {
  const result = generalSchema.safeParse({
    tenantId: formData.get("tenantId"),
    name: formData.get("name"),
    creed: formData.get("creed"),
    description: formData.get("description") ?? undefined
  });
  if (!result.success) {
    return { error: "Invalid data" };
  }
  await updateTenantSettings(result.data.tenantId, {
    name: result.data.name,
    creed: result.data.creed,
    description: result.data.description
  });
  return { success: true };
}

const privacySchema = z.object({ tenantId: z.string(), isPublic: z.enum(["true", "false"]), membershipApprovalMode: z.enum(["OPEN", "APPROVAL_REQUIRED"]) });

export async function updatePrivacySettings(_: any, formData: FormData) {
  const result = privacySchema.safeParse({
    tenantId: formData.get("tenantId"),
    isPublic: formData.get("isPublic"),
    membershipApprovalMode: formData.get("membershipApprovalMode")
  });
  if (!result.success) return { error: "Invalid data" };
  await updateTenantSettings(result.data.tenantId, {
    isPublic: result.data.isPublic === "true",
    membershipApprovalMode: result.data.membershipApprovalMode
  });
  return { success: true };
}
