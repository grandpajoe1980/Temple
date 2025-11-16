"use client";

import { Session } from "next-auth";
import { signOut } from "next-auth/react";
import Link from "next/link";
import { useState } from "react";
import { ChevronDown } from "lucide-react";

export function UserMenu({ session }: { session: Session }) {
  const [open, setOpen] = useState(false);
  return (
    <div className="relative">
      <button
        type="button"
        className="flex items-center gap-2 rounded-full border border-sand-200 bg-white px-3 py-1 text-sm text-sand-700 shadow-sm"
        onClick={() => setOpen((prev) => !prev)}
        aria-haspopup="menu"
        aria-expanded={open}
      >
        <span>{session.user?.name ?? session.user?.email}</span>
        <ChevronDown className="h-4 w-4" />
      </button>
      {open && (
        <div className="absolute right-0 mt-2 w-48 rounded-xl border border-sand-200 bg-white p-2 shadow-lg" role="menu">
          <Link className="block rounded-lg px-3 py-2 text-sm hover:bg-sand-50" href="/profile/me">
            Profile
          </Link>
          <Link className="block rounded-lg px-3 py-2 text-sm hover:bg-sand-50" href="/t/create">
            Create tenant
          </Link>
          <button
            onClick={() => signOut({ callbackUrl: "/" })}
            className="block w-full rounded-lg px-3 py-2 text-left text-sm text-red-600 hover:bg-red-50"
          >
            Sign out
          </button>
        </div>
      )}
    </div>
  );
}
