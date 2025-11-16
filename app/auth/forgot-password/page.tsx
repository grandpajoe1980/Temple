import { ForgotPasswordForm } from "@/components/forms/auth/forgot-password-form";

export default function ForgotPasswordPage() {
  return (
    <div className="mx-auto max-w-md px-4 py-12">
      <h1 className="text-3xl font-semibold text-slate-800">Forgot password</h1>
      <p className="mb-6 text-slate-500">Enter your email to receive a reset link.</p>
      <ForgotPasswordForm />
    </div>
  );
}
