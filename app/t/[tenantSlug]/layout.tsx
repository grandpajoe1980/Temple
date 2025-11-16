import { ReactNode } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getTenantBySlug } from "@/services/tenantService";

interface TenantLayoutProps {
  children: ReactNode;
  params: { tenantSlug: string };
}

const tenantNav = [
  { href: "", label: "Home" },
  { href: "/calendar", label: "Calendar" },
  { href: "/posts", label: "Posts" },
  { href: "/sermons", label: "Sermons" },
  { href: "/podcasts", label: "Podcasts" },
  { href: "/books", label: "Books" },
  { href: "/members", label: "Members" },
  { href: "/chat", label: "Chat" },
  { href: "/settings", label: "Control Panel", adminOnly: true }
];

export const dynamic = "force-dynamic";

export default async function TenantLayout({ children, params }: TenantLayoutProps) {
  const tenant = await getTenantBySlug(params.tenantSlug);
  if (!tenant || !tenant.isActive) return notFound();

  return (
    <div>
      <div className="relative h-48 w-full overflow-hidden bg-sand-200">
        {tenant.branding?.bannerImageUrl && (
          <img src={tenant.branding.bannerImageUrl} alt="Banner" className="h-full w-full object-cover" />
        )}
        <div className="absolute inset-0 bg-gradient-to-r from-sand-800/80 to-sand-500/60" />
        <div className="absolute bottom-4 left-1/2 w-full max-w-5xl -translate-x-1/2 px-4 text-white">
          <h1 className="text-3xl font-semibold">{tenant.name}</h1>
          <p>{tenant.creed} · {tenant.city}</p>
        </div>
      </div>
      <nav className="sticky top-14 z-30 border-b border-sand-200 bg-white/95">
        <div className="mx-auto flex max-w-5xl gap-4 overflow-x-auto px-4 py-3 text-sm font-medium">
          {tenantNav.map((item) => (
            <Link key={item.href} href={`/t/${tenant.slug}${item.href}`} className="text-slate-600 hover:text-sand-700">
              {item.label}
            </Link>
          ))}
        </div>
      </nav>
      <div className="bg-sand-50">
        {children}
      </div>
    </div>
  );
}
