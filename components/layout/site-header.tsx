"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Bell, Search } from "lucide-react";
import { Session } from "next-auth";
import { UserMenu } from "./user-menu";
import { Button } from "@/components/ui/button";

interface Props {
  session: Session | null;
}

const navLinks = [
  { href: "/explore", label: "Explore" },
  { href: "/messages", label: "Messages", authOnly: true },
  { href: "/admin", label: "Admin", superOnly: true }
];

export function SiteHeader({ session }: Props) {
  const pathname = usePathname();
  const isSuperAdmin = session?.user?.isSuperAdmin;

  return (
    <header className="sticky top-0 z-40 border-b border-sand-200 bg-white/95 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
        <Link href="/" className="text-2xl font-semibold text-sand-700">
          Temple
        </Link>
        <nav className="flex items-center gap-6 text-sm font-medium text-slate-600">
          {navLinks.map((link) => {
            if (link.superOnly && !isSuperAdmin) return null;
            if (link.authOnly && !session) return null;
            const isActive = pathname.startsWith(link.href);
            return (
              <Link
                key={link.href}
                href={link.href}
                className={
                  isActive
                    ? "text-sand-700"
                    : "text-slate-500 hover:text-slate-800"
                }
              >
                {link.label}
              </Link>
            );
          })}
        </nav>
        <div className="flex items-center gap-3">
          <Link
            href="/explore"
            className="rounded-full bg-sand-100 p-2 text-sand-600 hover:bg-sand-200"
            aria-label="Search tenants"
          >
            <Search className="h-4 w-4" />
          </Link>
          <Button variant="ghost" size="icon" aria-label="Notifications">
            <Bell className="h-5 w-5" />
          </Button>
          {session ? (
            <UserMenu session={session} />
          ) : (
            <div className="flex gap-2">
              <Button asChild variant="ghost">
                <Link href="/auth/login">Login</Link>
              </Button>
              <Button asChild>
                <Link href="/auth/register">Get started</Link>
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
