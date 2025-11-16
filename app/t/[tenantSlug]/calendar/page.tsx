import { db } from "@/lib/db";
import { notFound } from "next/navigation";

interface CalendarPageProps {
  params: { tenantSlug: string };
}

export default async function CalendarPage({ params }: CalendarPageProps) {
  const tenant = await db.tenant.findUnique({ where: { slug: params.tenantSlug }, include: { settings: true } });
  if (!tenant) return notFound();
  const events = await db.event.findMany({ where: { tenantId: tenant.id }, orderBy: { startDateTime: "asc" } });

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <h2 className="text-3xl font-semibold text-slate-800">Calendar</h2>
      <div className="mt-6 space-y-3">
        {events.map((event) => (
          <div key={event.id} className="rounded-3xl border border-sand-100 bg-white p-4">
            <h3 className="text-lg font-semibold text-slate-800">{event.title}</h3>
            <p className="text-sm text-slate-500">{new Date(event.startDateTime).toLocaleString()} · {event.locationText ?? "TBD"}</p>
            <p className="mt-2 text-slate-600">{event.description}</p>
          </div>
        ))}
        {events.length === 0 && <p className="text-slate-500">No events scheduled yet.</p>}
      </div>
    </div>
  );
}
