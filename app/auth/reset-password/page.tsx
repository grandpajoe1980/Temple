import { ResetPasswordForm } from "@/components/forms/auth/reset-password-form";

interface ResetPageProps {
  searchParams: { token?: string };
}

export default function ResetPasswordPage({ searchParams }: ResetPageProps) {
  const token = typeof searchParams.token === "string" ? searchParams.token : undefined;
  return (
    <div className="mx-auto max-w-md px-4 py-12">
      <h1 className="text-3xl font-semibold text-slate-800">Reset password</h1>
      <p className="mb-6 text-slate-500">Set a new password to continue.</p>
      <ResetPasswordForm token={token} />
    </div>
  );
}
