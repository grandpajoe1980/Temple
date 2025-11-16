"use server";

import { z } from "zod";
import { db } from "@/lib/db";
import { hashPassword } from "@/lib/password";
import { sendPasswordResetEmail } from "@/services/emailService";
import { randomBytes } from "crypto";

const registerSchema = z.object({
  email: z.string().email(),
  password: z.string().min(8),
  displayName: z.string().min(2)
});

export async function registerAction(prevState: { error?: string } | undefined, formData: FormData) {
  const input = registerSchema.safeParse({
    email: formData.get("email"),
    password: formData.get("password"),
    displayName: formData.get("displayName")
  });

  if (!input.success) {
    return { error: "Invalid form data" };
  }

  const existing = await db.user.findUnique({ where: { email: input.data.email.toLowerCase() } });
  if (existing) {
    return { error: "Email already in use" };
  }

  const passwordHash = await hashPassword(input.data.password);
  await db.user.create({
    data: {
      email: input.data.email.toLowerCase(),
      passwordHash,
      profile: {
        create: {
          displayName: input.data.displayName
        }
      },
      privacySettings: {
        create: {}
      },
      notificationPreference: {
        create: {}
      }
    }
  });

  return { success: true };
}

const forgotSchema = z.object({ email: z.string().email() });

export async function forgotPasswordAction(prev: { error?: string; success?: boolean } | undefined, formData: FormData) {
  const input = forgotSchema.safeParse({ email: formData.get("email") });
  if (!input.success) return { error: "Enter a valid email" };
  const user = await db.user.findUnique({ where: { email: input.data.email.toLowerCase() } });
  if (!user) return { success: true };
  const token = randomBytes(32).toString("hex");
  await db.passwordResetToken.create({
    data: {
      token,
      userId: user.id,
      expiresAt: new Date(Date.now() + 1000 * 60 * 60)
    }
  });
  const url = `${process.env.NEXTAUTH_URL ?? "http://localhost:3000"}/auth/reset-password?token=${token}`;
  await sendPasswordResetEmail(user.email, url);
  return { success: true };
}

const resetSchema = z.object({ token: z.string().min(10), password: z.string().min(8) });

export async function resetPasswordAction(prev: { error?: string; success?: boolean } | undefined, formData: FormData) {
  const input = resetSchema.safeParse({
    token: formData.get("token"),
    password: formData.get("password")
  });
  if (!input.success) return { error: "Invalid token" };
  const record = await db.passwordResetToken.findUnique({ where: { token: input.data.token } });
  if (!record || record.usedAt || record.expiresAt < new Date()) {
    return { error: "Token expired" };
  }
  const passwordHash = await hashPassword(input.data.password);
  await db.$transaction([
    db.user.update({ where: { id: record.userId }, data: { passwordHash } }),
    db.passwordResetToken.update({ where: { id: record.id }, data: { usedAt: new Date() } })
  ]);
  return { success: true };
}
