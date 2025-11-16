"use server";

import { z } from "zod";
import { createTenant } from "@/services/tenantService";
import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { redirect } from "next/navigation";

const schema = z.object({
  name: z.string().min(3),
  creed: z.string().min(2),
  city: z.string().min(2),
  country: z.string().min(2),
  description: z.string().optional()
});

export async function createTenantAction(_: any, formData: FormData) {
  const session = await getServerSession(authOptions);
  if (!session?.user?.id) {
    redirect("/auth/login");
  }

  const input = schema.safeParse({
    name: formData.get("name"),
    creed: formData.get("creed"),
    city: formData.get("city"),
    country: formData.get("country"),
    description: formData.get("description") ?? undefined
  });

  if (!input.success) {
    return { error: "Fill out all required fields" };
  }

  const tenant = await createTenant({
    ...input.data,
    createdByUserId: session!.user!.id,
    timeZone: "UTC"
  });

  redirect(`/t/${tenant.slug}/settings`);
}
