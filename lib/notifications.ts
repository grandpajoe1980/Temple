import { db } from "@/lib/db";
import { NotificationTypes } from "@/lib/constants";
import { sendNewDmEmail, sendAnnouncementEmail } from "@/services/emailService";

export type NotificationType = (typeof NotificationTypes)[number];

interface CreateNotificationInput {
  userId: string;
  type: NotificationType;
  entityType?: string;
  entityId?: string;
  tenantId?: string;
  emailFallback?: {
    to: string;
    url: string;
    title: string;
  };
}

export async function createNotification(input: CreateNotificationInput) {
  const notification = await db.notification.create({
    data: {
      userId: input.userId,
      type: input.type,
      entityId: input.entityId,
      entityType: input.entityType,
      tenantId: input.tenantId
    }
  });

  if (input.emailFallback) {
    if (input.type === "NEW_DM") {
      await sendNewDmEmail(input.emailFallback.to, input.emailFallback.title, input.emailFallback.url);
    }
    if (input.type === "NEW_TENANT_ANNOUNCEMENT") {
      await sendAnnouncementEmail(input.emailFallback.to, input.emailFallback.title, input.emailFallback.url);
    }
  }

  return notification;
}
