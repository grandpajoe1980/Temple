import { db } from "@/lib/db";
import Link from "next/link";
import { notFound } from "next/navigation";

interface TenantPostsPageProps {
  params: { tenantSlug: string };
}

export default async function TenantPostsPage({ params }: TenantPostsPageProps) {
  const tenant = await db.tenant.findUnique({ where: { slug: params.tenantSlug } });
  if (!tenant) return notFound();
  const posts = await db.post.findMany({
    where: { tenantId: tenant.id, visibility: "PUBLIC" },
    orderBy: { createdAt: "desc" },
    take: 20
  });

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <div className="flex items-center justify-between">
        <h2 className="text-3xl font-semibold text-slate-800">Posts & announcements</h2>
        <span className="rounded-full border border-dashed border-sand-300 px-4 py-2 text-sm text-slate-500">
          Creation tools coming soon
        </span>
      </div>
      <div className="mt-6 space-y-4">
        {posts.map((post) => (
          <Link key={post.id} href={`/t/${tenant.slug}/posts/${post.id}`} className="block rounded-3xl border border-sand-100 bg-white p-6">
            <h3 className="text-xl font-semibold text-slate-800">{post.title}</h3>
            <p className="text-sm text-slate-500">{new Date(post.createdAt).toLocaleDateString()}</p>
            <p className="mt-2 text-slate-600 line-clamp-3">{post.body.slice(0, 200)}</p>
          </Link>
        ))}
        {posts.length === 0 && <p className="text-slate-500">No posts yet.</p>}
      </div>
    </div>
  );
}
