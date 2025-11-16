import { notFound } from "next/navigation";
import { db } from "@/lib/db";
import Link from "next/link";

interface ProfilePageProps {
  params: { userIdOrSlug: string };
}

export default async function ProfilePage({ params }: ProfilePageProps) {
  const user = await db.user.findFirst({
    where: {
      OR: [{ id: params.userIdOrSlug }, { email: params.userIdOrSlug }]
    },
    include: {
      profile: true,
      memberships: {
        include: {
          tenant: true,
          roles: true
        },
        where: { status: "APPROVED" }
      }
    }
  });

  if (!user) return notFound();

  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <div className="rounded-3xl border border-sand-100 bg-white p-6">
        <h1 className="text-3xl font-semibold text-slate-800">{user.profile?.displayName ?? user.email}</h1>
        <p className="text-slate-500">{user.profile?.bio ?? "This member has not shared a bio yet."}</p>
        <div className="mt-6">
          <h2 className="text-lg font-semibold">Affiliations</h2>
          <ul className="mt-3 space-y-2">
            {user.memberships.map((membership) => (
              <li key={membership.id} className="rounded-2xl bg-sand-50 p-4">
                <div className="flex items-center justify-between">
                  <Link href={`/t/${membership.tenant.slug}`} className="text-sand-700 underline">
                    {membership.tenant.name}
                  </Link>
                  <p className="text-sm text-slate-500">
                    {membership.roles.find((r) => r.isPrimary)?.displayTitle ?? membership.roles[0]?.role ?? "Member"}
                  </p>
                </div>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
