import { db } from "@/lib/db";
import { createNotification } from "@/lib/notifications";

export async function markNotificationRead(notificationId: string, userId: string) {
  return db.notification.update({
    where: { id: notificationId, userId },
    data: { isRead: true }
  });
}

export async function listNotifications(userId: string) {
  return db.notification.findMany({
    where: { userId },
    orderBy: { createdAt: "desc" },
    take: 20
  });
}

export async function notifyNewDm({ targetUserId, conversationId, senderName, email }: { targetUserId: string; conversationId: string; senderName: string; email?: string }) {
  await createNotification({
    userId: targetUserId,
    type: "NEW_DM",
    entityType: "MESSAGE",
    entityId: conversationId,
    emailFallback: email
      ? {
          to: email,
          url: `${process.env.NEXTAUTH_URL ?? "http://localhost:3000"}/messages/${conversationId}`,
          title: `${senderName} sent you a message`
        }
      : undefined
  });
}
