import { cn } from "@/lib/utils";
import { HTMLAttributes } from "react";

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-3xl border border-sand-100 bg-white p-4 shadow-sm",
        className
      )}
      {...props}
    />
  );
}
