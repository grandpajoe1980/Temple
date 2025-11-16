import { forwardRef, InputHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, ...props }, ref) => (
    <input
      ref={ref}
      className={cn(
        "w-full rounded-xl border border-sand-200 bg-white px-4 py-2 text-sm text-slate-800 shadow-sm placeholder:text-slate-400 focus:border-sand-500 focus:outline-none focus:ring-2 focus:ring-sand-100",
        className
      )}
      {...props}
    />
  )
);
Input.displayName = "Input";
