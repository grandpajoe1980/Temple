"use client";

import { useFormState } from "react-dom";
import { resetPasswordAction } from "@/app/auth/actions";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

export function ResetPasswordForm({ token }: { token?: string }) {
  const [state, formAction] = useFormState(resetPasswordAction, { success: false } as { error?: string; success?: boolean });
  return (
    <form action={formAction} className="space-y-4">
      <input type="hidden" name="token" value={token} />
      <div>
        <label className="text-sm text-slate-600">New password</label>
        <Input type="password" name="password" required minLength={8} />
      </div>
      {state?.error && <p className="text-sm text-red-600">{state.error}</p>}
      {state?.success && <p className="text-sm text-green-600">Password updated. You can now login.</p>}
      <Button type="submit" className="w-full">
        Reset password
      </Button>
    </form>
  );
}
