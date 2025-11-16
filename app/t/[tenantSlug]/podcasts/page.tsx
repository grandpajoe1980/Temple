import { db } from "@/lib/db";
import { notFound } from "next/navigation";

interface PodcastsPageProps {
  params: { tenantSlug: string };
}

export default async function PodcastsPage({ params }: PodcastsPageProps) {
  const tenant = await db.tenant.findUnique({ where: { slug: params.tenantSlug } });
  if (!tenant) return notFound();
  const podcasts = await db.mediaItem.findMany({
    where: { tenantId: tenant.id, type: "PODCAST_AUDIO" },
    orderBy: { publishedAt: "desc" }
  });

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <h2 className="text-3xl font-semibold text-slate-800">Podcasts</h2>
      <div className="mt-6 space-y-4">
        {podcasts.map((item) => (
          <article key={item.id} className="rounded-3xl border border-sand-100 bg-white p-4">
            <h3 className="text-xl font-semibold text-slate-800">{item.title}</h3>
            <p className="text-sm text-slate-500">{item.publishedAt ? new Date(item.publishedAt).toLocaleDateString() : null}</p>
            <p className="mt-2 text-slate-600">{item.description}</p>
            <a href={item.embedUrl} target="_blank" rel="noreferrer" className="text-sand-600 underline">
              Listen now
            </a>
          </article>
        ))}
        {podcasts.length === 0 && <p className="text-slate-500">No podcasts yet.</p>}
      </div>
    </div>
  );
}
