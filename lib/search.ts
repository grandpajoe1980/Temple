import { db } from "@/lib/db";

export interface TenantSearchInput {
  query?: string;
  creed?: string;
  city?: string;
  language?: string;
}

export async function searchTenants({ query, creed, city, language }: TenantSearchInput) {
  return db.tenant.findMany({
    where: {
      isActive: true,
      settings: { isPublic: true },
      AND: [
        query
          ? {
              OR: [
                { name: { contains: query, mode: "insensitive" } },
                { description: { contains: query, mode: "insensitive" } },
                { creed: { contains: query, mode: "insensitive" } }
              ]
            }
          : {},
        creed ? { creed: { equals: creed, mode: "insensitive" } } : {},
        city ? { city: { equals: city, mode: "insensitive" } } : {}
      ],
      ...(language
        ? {
            OR: [
              { defaultLanguage: { equals: language, mode: "insensitive" } },
              { branding: { customLinksJson: { contains: language } } }
            ]
          }
        : {})
    },
    include: {
      settings: true
    },
    orderBy: {
      createdAt: "desc"
    }
  });
}
