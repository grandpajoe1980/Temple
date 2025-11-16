import { db } from "@/lib/db";
import { notFound } from "next/navigation";

interface PostDetailProps {
  params: { tenantSlug: string; postId: string };
}

export default async function PostDetailPage({ params }: PostDetailProps) {
  const tenant = await db.tenant.findUnique({ where: { slug: params.tenantSlug } });
  if (!tenant) return notFound();
  const post = await db.post.findFirst({ where: { id: params.postId, tenantId: tenant.id }, include: { author: { select: { email: true, profile: true } }, comments: { include: { author: true } } } });
  if (!post) return notFound();

  return (
    <div className="mx-auto max-w-4xl px-4 py-10">
      <article className="rounded-3xl border border-sand-100 bg-white p-6">
        <h1 className="text-3xl font-semibold text-slate-800">{post.title}</h1>
        <p className="text-sm text-slate-500">{new Date(post.createdAt).toLocaleDateString()} · {post.author.profile?.displayName ?? post.author.email}</p>
        <div className="prose prose-slate mt-6" dangerouslySetInnerHTML={{ __html: post.body }} />
      </article>
      <section className="mt-6 rounded-3xl border border-sand-100 bg-white p-6">
        <h2 className="text-xl font-semibold">Comments</h2>
        <div className="mt-4 space-y-3">
          {post.comments.map((comment) => (
            <div key={comment.id} className="rounded-2xl bg-sand-50 p-3 text-sm">
              <p className="font-medium text-slate-700">{comment.author.email}</p>
              <p className="text-slate-600">{comment.body}</p>
            </div>
          ))}
          {post.comments.length === 0 && <p className="text-slate-500">No comments yet.</p>}
        </div>
      </section>
    </div>
  );
}
