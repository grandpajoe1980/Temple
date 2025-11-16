import { db } from "@/lib/db";
import { slugify } from "@/services/utility";
import { MembershipApprovalModes, TenantRoles } from "@/lib/constants";

interface CreateTenantInput {
  name: string;
  creed: string;
  city: string;
  state?: string;
  country: string;
  postalCode?: string;
  description?: string;
  createdByUserId: string;
  timeZone?: string;
}

export async function createTenant(input: CreateTenantInput) {
  const slug = slugify(input.name);
  const uniqueSlug = await resolveUniqueSlug(slug);

  return db.$transaction(async (tx) => {
    const tenant = await tx.tenant.create({
      data: {
        name: input.name,
        slug: uniqueSlug,
        creed: input.creed,
        city: input.city,
        state: input.state,
        country: input.country,
        postalCode: input.postalCode,
        description: input.description,
        timeZone: input.timeZone ?? "UTC",
        createdByUserId: input.createdByUserId
      }
    });

    await tx.tenantSettings.create({
      data: {
        tenantId: tenant.id,
        membershipApprovalMode: MembershipApprovalModes[0]
      }
    });

    for (const role of TenantRoles) {
      await tx.tenantFeaturePermissions.create({
        data: {
          tenantId: tenant.id,
          roleType: role,
          canCreatePosts: role !== "MEMBER",
          canCreateEvents: role !== "MEMBER",
          canInviteMembers: role === "ADMIN" || role === "MODERATOR",
          canApproveMembership: role === "ADMIN" || role === "MODERATOR",
          canModerateContent: role === "ADMIN" || role === "MODERATOR"
        }
      });
    }

    await tx.userTenantMembership.create({
      data: {
        userId: input.createdByUserId,
        tenantId: tenant.id,
        status: "APPROVED",
        roles: {
          create: [{ role: "ADMIN", isPrimary: true }]
        }
      }
    });

    await tx.conversation.createMany({
      data: [
        {
          tenantId: tenant.id,
          name: "Announcements",
          isDefaultChannel: true,
          isPrivateGroup: false,
          createdByUserId: input.createdByUserId
        },
        {
          tenantId: tenant.id,
          name: "General",
          isDefaultChannel: true,
          isPrivateGroup: false,
          createdByUserId: input.createdByUserId
        }
      ]
    });

    return tenant;
  });
}

export async function resolveUniqueSlug(baseSlug: string) {
  let slug = baseSlug;
  let counter = 1;
  while (await db.tenant.findUnique({ where: { slug } })) {
    slug = `${baseSlug}-${counter++}`;
  }
  return slug;
}

export async function listTenantsForUser(userId: string) {
  return db.userTenantMembership.findMany({
    where: { userId, status: "APPROVED" },
    include: {
      tenant: true,
      roles: true
    }
  });
}

export async function getTenantBySlug(slug: string) {
  return db.tenant.findUnique({
    where: { slug },
    include: {
      branding: true,
      settings: true
    }
  });
}

export async function updateTenantSettings(
  tenantId: string,
  data: Partial<{ name: string; description: string; creed: string; membershipApprovalMode: string; isPublic: boolean }>
) {
  const { membershipApprovalMode, isPublic, ...general } = data;
  if (Object.keys(general).length) {
    await db.tenant.update({ where: { id: tenantId }, data: general });
  }
  if (membershipApprovalMode || typeof isPublic === "boolean") {
    await db.tenantSettings.update({
      where: { tenantId },
      data: {
        ...(membershipApprovalMode ? { membershipApprovalMode } : {}),
        ...(typeof isPublic === "boolean" ? { isPublic } : {})
      }
    });
  }
}
