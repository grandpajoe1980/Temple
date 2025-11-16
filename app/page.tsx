import { Suspense } from "react";
import Link from "next/link";
import { SearchForm } from "@/components/tenants/search-form";

export default function LandingPage() {
  return (
    <div className="bg-gradient-to-b from-sand-100 to-sand-50">
      <section className="mx-auto flex max-w-5xl flex-col items-center gap-6 px-4 py-24 text-center">
        <h1 className="text-4xl font-semibold text-sand-800 sm:text-5xl">Find your temple, Find yourself</h1>
        <p className="max-w-2xl text-lg text-slate-600">
          Temple is a warm, multi-tenant platform for faith communities to welcome seekers, share schedules, sermons, and keep every member connected.
        </p>
        <Suspense fallback={<div className="h-16 w-full rounded-3xl bg-white/60" />}>
          <SearchForm />
        </Suspense>
        <div className="flex gap-4 text-sm text-slate-600">
          <Link href="/auth/login" className="underline">
            Login
          </Link>
          <Link href="/auth/register" className="underline">
            Register
          </Link>
        </div>
      </section>
    </div>
  );
}
