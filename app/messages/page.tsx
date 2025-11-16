import { getServerSession } from "next-auth";
import Link from "next/link";
import { authOptions } from "@/lib/auth";
import { listConversationsForUser } from "@/services/messagingService";

export const dynamic = "force-dynamic";

export default async function MessagesPage() {
  const session = await getServerSession(authOptions);
  if (!session?.user?.id) {
    return (
      <div className="mx-auto max-w-3xl px-4 py-12 text-center">
        <p className="text-slate-600">
          Please <Link className="text-sand-600 underline" href="/auth/login">login</Link> to read or send messages.
        </p>
      </div>
    );
  }

  const conversations = await listConversationsForUser(session.user.id);

  return (
    <div className="mx-auto grid max-w-6xl gap-6 px-4 py-10 lg:grid-cols-[280px,1fr]">
      <aside className="rounded-3xl border border-sand-100 bg-white p-4">
        <h2 className="text-lg font-semibold text-slate-800">Conversations</h2>
        <input
          className="mt-3 w-full rounded-2xl border border-sand-100 px-3 py-2 text-sm"
          placeholder="Search"
          aria-label="Search conversations"
        />
        <ul className="mt-4 space-y-2">
          {conversations.map((conversation) => (
            <li key={conversation.id} className="rounded-2xl bg-sand-50 p-3 text-sm">
              <p className="font-medium text-slate-700">{conversation.name ?? "Direct message"}</p>
              <p className="text-slate-500">
                {conversation.messages[0]?.body?.slice(0, 60) ?? "Say hello"}
              </p>
            </li>
          ))}
        </ul>
      </aside>
      <section className="rounded-3xl border border-sand-100 bg-white p-6">
        <p className="text-slate-500">Select a conversation from the left to get started.</p>
      </section>
    </div>
  );
}
