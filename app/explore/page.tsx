import { searchTenants } from "@/lib/search";
import { TenantCard } from "@/components/tenants/tenant-card";

interface ExplorePageProps {
  searchParams: { [key: string]: string | string[] | undefined };
}

export default async function ExplorePage({ searchParams }: ExplorePageProps) {
  const tenants = await searchTenants({
    query: typeof searchParams.query === "string" ? searchParams.query : undefined,
    creed: typeof searchParams.creed === "string" ? searchParams.creed : undefined,
    city: typeof searchParams.city === "string" ? searchParams.city : undefined,
    language: typeof searchParams.language === "string" ? searchParams.language : undefined
  });

  return (
    <div className="mx-auto max-w-5xl px-4 py-10">
      <h2 className="text-3xl font-semibold text-slate-800">Explore temples</h2>
      <p className="text-slate-500">Search results are filtered by tenant privacy settings.</p>
      <div className="mt-6 grid gap-4">
        {tenants.length === 0 && <p className="text-slate-500">No temples match your filters yet.</p>}
        {tenants.map((tenant) => (
          <TenantCard
            key={tenant.id}
            slug={tenant.slug}
            name={tenant.name}
            creed={tenant.creed}
            city={tenant.city}
            country={tenant.country}
            description={tenant.description}
            languages={tenant.branding?.customLinksJson ? JSON.parse(tenant.branding.customLinksJson) : undefined}
          />
        ))}
      </div>
    </div>
  );
}
