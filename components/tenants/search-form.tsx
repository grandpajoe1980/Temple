"use client";

import { useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { SlidersHorizontal } from "lucide-react";

export function SearchForm() {
  const router = useRouter();
  const [showFilters, setShowFilters] = useState(false);

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const params = new URLSearchParams();
    for (const [key, value] of formData.entries()) {
      if (typeof value === "string" && value.trim().length > 0) {
        params.append(key, value);
      }
    }
    router.push(`/explore?${params.toString()}`);
  }

  return (
    <form onSubmit={onSubmit} className="w-full max-w-3xl rounded-3xl bg-white p-6 shadow-xl">
      <div className="flex items-center gap-3">
        <Input name="query" placeholder="Search by name, creed, or city" />
        <Button type="button" variant="secondary" size="icon" onClick={() => setShowFilters((prev) => !prev)}>
          <SlidersHorizontal className="h-5 w-5" />
        </Button>
        <Button type="submit">Search</Button>
      </div>
      {showFilters && (
        <div className="mt-4 grid gap-3 sm:grid-cols-3">
          <Input name="creed" placeholder="Creed" />
          <Input name="city" placeholder="City or ZIP" />
          <Input name="language" placeholder="Language" />
        </div>
      )}
    </form>
  );
}
