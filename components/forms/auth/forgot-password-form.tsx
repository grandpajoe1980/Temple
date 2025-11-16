"use client";

import { useFormState } from "react-dom";
import { forgotPasswordAction } from "@/app/auth/actions";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

export function ForgotPasswordForm() {
  const [state, formAction] = useFormState(forgotPasswordAction, { success: false });
  return (
    <form action={formAction} className="space-y-4">
      <div>
        <label className="text-sm text-slate-600">Email</label>
        <Input type="email" name="email" required />
      </div>
      {state?.error && <p className="text-sm text-red-600">{state.error}</p>}
      {state?.success && <p className="text-sm text-green-600">Check your inbox for reset instructions.</p>}
      <Button type="submit" className="w-full">
        Send reset link
      </Button>
    </form>
  );
}
