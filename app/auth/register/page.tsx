import Link from "next/link";
import { RegisterForm } from "@/components/forms/auth/register-form";

export default function RegisterPage() {
  return (
    <div className="mx-auto flex max-w-md flex-col gap-6 px-4 py-12">
      <div>
        <h1 className="text-3xl font-semibold text-slate-800">Create your Temple account</h1>
        <p className="text-slate-500">One global identity, countless communities.</p>
      </div>
      <RegisterForm />
      <p className="text-sm text-slate-500">
        Already a member? <Link href="/auth/login" className="text-sand-600">Sign in</Link>
      </p>
    </div>
  );
}
