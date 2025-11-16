"use client";

import { useFormState } from "react-dom";
import { registerAction } from "@/app/auth/actions";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const initialState = { error: undefined, success: false } as { error?: string; success?: boolean };

export function RegisterForm() {
  const [state, formAction] = useFormState(registerAction, initialState);
  return (
    <form action={formAction} className="space-y-4">
      <div>
        <label className="text-sm text-slate-600">Display name</label>
        <Input name="displayName" required minLength={2} />
      </div>
      <div>
        <label className="text-sm text-slate-600">Email</label>
        <Input type="email" name="email" required autoComplete="email" />
      </div>
      <div>
        <label className="text-sm text-slate-600">Password</label>
        <Input type="password" name="password" required autoComplete="new-password" minLength={8} />
      </div>
      {state?.error && <p className="text-sm text-red-600">{state.error}</p>}
      {state?.success && <p className="text-sm text-green-600">Account created! You can now log in.</p>}
      <Button type="submit" className="w-full">
        Create account
      </Button>
    </form>
  );
}
