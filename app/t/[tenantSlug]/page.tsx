import { getTenantBySlug } from "@/services/tenantService";
import { notFound } from "next/navigation";
import Link from "next/link";

interface TenantHomeProps {
  params: { tenantSlug: string };
}

export default async function TenantHomePage({ params }: TenantHomeProps) {
  const tenant = await getTenantBySlug(params.tenantSlug);
  if (!tenant || !tenant.isActive) return notFound();

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <div className="grid gap-6 lg:grid-cols-[2fr,1fr]">
        <section className="rounded-3xl border border-sand-100 bg-white p-6">
          <h2 className="text-2xl font-semibold text-slate-800">About</h2>
          <p className="mt-2 text-slate-600">{tenant.description ?? "This temple has not shared its story yet."}</p>
          <div className="mt-6 flex gap-4">
            <Link href={`/t/${tenant.slug}/posts`} className="rounded-full bg-sand-600 px-4 py-2 text-white">
              Recent posts
            </Link>
            <Link href={`/t/${tenant.slug}/calendar`} className="rounded-full border border-sand-200 px-4 py-2 text-sand-700">
              Upcoming events
            </Link>
          </div>
        </section>
        <aside className="space-y-4">
          <div className="rounded-3xl border border-sand-100 bg-white p-4">
            <h3 className="text-lg font-semibold text-slate-700">Quick info</h3>
            <dl className="mt-3 space-y-2 text-sm text-slate-600">
              <div>
                <dt className="font-medium">Creed</dt>
                <dd>{tenant.creed}</dd>
              </div>
              <div>
                <dt className="font-medium">Location</dt>
                <dd>
                  {tenant.city}, {tenant.country}
                </dd>
              </div>
            </dl>
          </div>
        </aside>
      </div>
    </div>
  );
}
