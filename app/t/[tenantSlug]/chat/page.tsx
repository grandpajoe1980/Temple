import { db } from "@/lib/db";
import { notFound } from "next/navigation";

interface ChatPageProps {
  params: { tenantSlug: string };
}

export default async function ChatPage({ params }: ChatPageProps) {
  const tenant = await db.tenant.findUnique({ where: { slug: params.tenantSlug } });
  if (!tenant) return notFound();
  const conversations = await db.conversation.findMany({
    where: { tenantId: tenant.id },
    include: { messages: { take: 5, orderBy: { createdAt: "desc" }, include: { sender: true } } }
  });

  return (
    <div className="mx-auto grid max-w-6xl gap-6 px-4 py-10 lg:grid-cols-[280px,1fr]">
      <aside className="rounded-3xl border border-sand-100 bg-white p-4">
        <h2 className="text-lg font-semibold text-slate-800">Channels</h2>
        <ul className="mt-4 space-y-2 text-sm">
          {conversations.map((conversation) => (
            <li key={conversation.id} className="rounded-2xl bg-sand-50 p-3">
              {conversation.name ?? "Group"}
            </li>
          ))}
        </ul>
      </aside>
      <section className="rounded-3xl border border-sand-100 bg-white p-6">
        <p className="text-slate-500">Select a channel to read messages.</p>
      </section>
    </div>
  );
}
