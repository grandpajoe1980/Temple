import { getServerSession } from "next-auth";
import { notFound, redirect } from "next/navigation";
import { authOptions } from "@/lib/auth";
import { db } from "@/lib/db";
import { updateGeneralSettings, updatePrivacySettings } from "./actions";

interface SettingsPageProps {
  params: { tenantSlug: string };
}

export default async function TenantSettingsPage({ params }: SettingsPageProps) {
  const session = await getServerSession(authOptions);
  if (!session?.user?.id) redirect("/auth/login");
  const tenant = await db.tenant.findUnique({
    where: { slug: params.tenantSlug },
    include: { settings: true }
  });
  if (!tenant) return notFound();

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <h1 className="text-3xl font-semibold text-slate-800">Control Panel</h1>
      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <form action={updateGeneralSettings} className="rounded-3xl border border-sand-100 bg-white p-6">
          <input type="hidden" name="tenantId" value={tenant.id} />
          <h2 className="text-xl font-semibold text-slate-800">General</h2>
          <label className="mt-4 block text-sm text-slate-600">Name</label>
          <input name="name" defaultValue={tenant.name} className="mt-1 w-full rounded-2xl border border-sand-100 px-3 py-2" />
          <label className="mt-4 block text-sm text-slate-600">Creed</label>
          <input name="creed" defaultValue={tenant.creed} className="mt-1 w-full rounded-2xl border border-sand-100 px-3 py-2" />
          <label className="mt-4 block text-sm text-slate-600">Description</label>
          <textarea
            name="description"
            defaultValue={tenant.description ?? ""}
            className="mt-1 w-full rounded-2xl border border-sand-100 px-3 py-2"
          ></textarea>
          <button type="submit" className="mt-4 rounded-full bg-sand-600 px-4 py-2 text-white">
            Save general
          </button>
        </form>
        <form action={updatePrivacySettings} className="rounded-3xl border border-sand-100 bg-white p-6">
          <input type="hidden" name="tenantId" value={tenant.id} />
          <h2 className="text-xl font-semibold text-slate-800">Visibility & membership</h2>
          <label className="mt-4 block text-sm text-slate-600">Public listing</label>
          <select name="isPublic" defaultValue={tenant.settings?.isPublic ? "true" : "false"} className="mt-1 w-full rounded-2xl border border-sand-100 px-3 py-2">
            <option value="true">Public</option>
            <option value="false">Private</option>
          </select>
          <label className="mt-4 block text-sm text-slate-600">Membership approval</label>
          <select name="membershipApprovalMode" defaultValue={tenant.settings?.membershipApprovalMode ?? "OPEN"} className="mt-1 w-full rounded-2xl border border-sand-100 px-3 py-2">
            <option value="OPEN">Open</option>
            <option value="APPROVAL_REQUIRED">Approval required</option>
          </select>
          <button type="submit" className="mt-4 rounded-full bg-sand-600 px-4 py-2 text-white">
            Save privacy
          </button>
        </form>
      </div>
    </div>
  );
}
