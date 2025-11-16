import { db } from "@/lib/db";
import { notFound } from "next/navigation";

interface BooksPageProps {
  params: { tenantSlug: string };
}

export default async function BooksPage({ params }: BooksPageProps) {
  const tenant = await db.tenant.findUnique({ where: { slug: params.tenantSlug } });
  if (!tenant) return notFound();
  const books = await db.post.findMany({ where: { tenantId: tenant.id, type: "BOOK" }, orderBy: { createdAt: "desc" } });

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <h2 className="text-3xl font-semibold text-slate-800">Books & long-form</h2>
      <div className="mt-6 space-y-4">
        {books.map((book) => (
          <article key={book.id} className="rounded-3xl border border-sand-100 bg-white p-6">
            <h3 className="text-2xl font-semibold text-slate-800">{book.title}</h3>
            <p className="text-sm text-slate-500">{new Date(book.createdAt).toLocaleDateString()}</p>
            <p className="mt-2 text-slate-600">{book.body.slice(0, 200)}...</p>
          </article>
        ))}
        {books.length === 0 && <p className="text-slate-500">No books yet.</p>}
      </div>
    </div>
  );
}
