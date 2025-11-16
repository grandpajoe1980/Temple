import { db } from "@/lib/db";
import { notFound } from "next/navigation";

interface MembersPageProps {
  params: { tenantSlug: string };
}

export default async function MembersPage({ params }: MembersPageProps) {
  const tenant = await db.tenant.findUnique({ where: { slug: params.tenantSlug }, include: { settings: true } });
  if (!tenant) return notFound();
  if (!tenant.settings?.enableMemberDirectory) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-10">
        <p className="text-slate-600">Member directory is disabled.</p>
      </div>
    );
  }
  const members = await db.userTenantMembership.findMany({
    where: { tenantId: tenant.id, status: "APPROVED" },
    include: { user: { include: { profile: true } }, roles: true }
  });

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <h2 className="text-3xl font-semibold text-slate-800">Members</h2>
      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        {members.map((member) => (
          <div key={member.id} className="rounded-3xl border border-sand-100 bg-white p-4">
            <h3 className="text-xl font-semibold text-slate-800">{member.user.profile?.displayName ?? member.user.email}</h3>
            <p className="text-sm text-slate-500">{member.roles.find((role) => role.isPrimary)?.displayTitle ?? member.roles[0]?.role ?? "Member"}</p>
          </div>
        ))}
        {members.length === 0 && <p className="text-slate-500">No members yet.</p>}
      </div>
    </div>
  );
}
