import Link from "next/link";
import { LoginForm } from "@/components/forms/auth/login-form";

export default function LoginPage() {
  return (
    <div className="mx-auto flex max-w-md flex-col gap-6 px-4 py-12">
      <div>
        <h1 className="text-3xl font-semibold text-slate-800">Welcome back</h1>
        <p className="text-slate-500">Sign in to continue.</p>
      </div>
      <LoginForm />
      <Link href="/auth/forgot-password" className="text-sm text-sand-600">
        Forgot your password?
      </Link>
    </div>
  );
}
