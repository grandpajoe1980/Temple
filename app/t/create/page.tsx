import { createTenantAction } from "./actions";
import { redirect } from "next/navigation";
import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";

export default async function CreateTenantPage() {
  const session = await getServerSession(authOptions);
  if (!session?.user?.id) {
    redirect("/auth/login");
  }

  return (
    <div className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-3xl font-semibold text-slate-800">Create a new tenant</h1>
      <form action={createTenantAction} className="mt-6 space-y-4 rounded-3xl border border-sand-100 bg-white p-6">
        <label className="block text-sm text-slate-600">Name</label>
        <input name="name" required className="w-full rounded-2xl border border-sand-100 px-3 py-2" />
        <label className="block text-sm text-slate-600">Creed</label>
        <input name="creed" required className="w-full rounded-2xl border border-sand-100 px-3 py-2" />
        <label className="block text-sm text-slate-600">City</label>
        <input name="city" required className="w-full rounded-2xl border border-sand-100 px-3 py-2" />
        <label className="block text-sm text-slate-600">Country</label>
        <input name="country" required className="w-full rounded-2xl border border-sand-100 px-3 py-2" />
        <label className="block text-sm text-slate-600">Description</label>
        <textarea name="description" className="w-full rounded-2xl border border-sand-100 px-3 py-2"></textarea>
        <button type="submit" className="rounded-full bg-sand-600 px-4 py-2 text-white">Create tenant</button>
      </form>
    </div>
  );
}
