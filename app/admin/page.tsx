import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";
import { db } from "@/lib/db";
import { redirect } from "next/navigation";

export default async function AdminPage() {
  const session = await getServerSession(authOptions);
  if (!session?.user?.isSuperAdmin) {
    redirect("/");
  }

  const tenants = await db.tenant.findMany({ orderBy: { createdAt: "desc" }, take: 20 });
  const users = await db.user.findMany({ orderBy: { createdAt: "desc" }, take: 20 });

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <h1 className="text-3xl font-semibold text-slate-800">Platform admin</h1>
      <section className="mt-6 rounded-3xl border border-sand-100 bg-white p-6">
        <h2 className="text-xl font-semibold">Tenants</h2>
        <ul className="mt-4 space-y-2">
          {tenants.map((tenant) => (
            <li key={tenant.id} className="flex items-center justify-between rounded-2xl bg-sand-50 p-4 text-sm">
              <span>
                {tenant.name} · {tenant.creed}
              </span>
              <span className="text-slate-500">{tenant.isActive ? "Active" : "Deactivated"}</span>
            </li>
          ))}
        </ul>
      </section>
      <section className="mt-6 rounded-3xl border border-sand-100 bg-white p-6">
        <h2 className="text-xl font-semibold">Recent users</h2>
        <ul className="mt-4 space-y-2 text-sm">
          {users.map((user) => (
            <li key={user.id} className="rounded-2xl bg-sand-50 p-4">
              {user.email}
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
