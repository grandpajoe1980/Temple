import "./globals.css";
import { ReactNode } from "react";
import { SiteHeader } from "@/components/layout/site-header";
import { SiteFooter } from "@/components/layout/site-footer";
import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth";

export const metadata = {
  title: "Temple",
  description: "Find your temple, find yourself"
};

export default async function RootLayout({ children }: { children: ReactNode }) {
  const session = await getServerSession(authOptions);

  return (
    <html lang="en" className="bg-sand-50">
      <body className="min-h-screen font-sans text-slate-900">
        <div className="flex min-h-screen flex-col">
          <SiteHeader session={session} />
          <main className="flex-1 bg-sand-50">{children}</main>
          <SiteFooter />
        </div>
      </body>
    </html>
  );
}
