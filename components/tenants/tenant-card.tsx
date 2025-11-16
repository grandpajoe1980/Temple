import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

interface TenantCardProps {
  slug: string;
  name: string;
  creed: string;
  city: string;
  country: string;
  description?: string | null;
  languages?: string[];
}

export function TenantCard({ slug, name, creed, city, country, description, languages }: TenantCardProps) {
  return (
    <Card className="flex flex-col gap-3 p-6">
      <div className="flex items-start justify-between">
        <div>
          <h3 className="text-xl font-semibold text-slate-800">{name}</h3>
          <p className="text-sm text-slate-500">{creed}</p>
        </div>
        <Badge>{city}, {country}</Badge>
      </div>
      {description && <p className="text-sm text-slate-600 line-clamp-3">{description}</p>}
      {languages && languages.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {languages.map((language) => (
            <Badge key={language} className="bg-white text-slate-600">
              {language}
            </Badge>
          ))}
        </div>
      )}
      <div className="mt-auto flex gap-3">
        <Link
          href={`/t/${slug}`}
          className="rounded-full bg-sand-600 px-4 py-2 text-sm font-medium text-white shadow hover:bg-sand-700"
        >
          View temple
        </Link>
        <Link
          href={`/t/${slug}?intent=join`}
          className="rounded-full border border-sand-200 px-4 py-2 text-sm font-medium text-sand-700"
        >
          Join or visit
        </Link>
      </div>
    </Card>
  );
}
