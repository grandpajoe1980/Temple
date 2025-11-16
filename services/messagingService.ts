import { db } from "@/lib/db";

export async function upsertDirectConversation(userA: string, userB: string) {
  const existing = await db.conversation.findFirst({
    where: {
      isDirect: true,
      participants: {
        some: { userId: userA }
      }
    },
    include: {
      participants: true
    }
  });

  if (existing && existing.participants.some((p) => p.userId === userB)) {
    return existing;
  }

  return db.conversation.create({
    data: {
      isDirect: true,
      createdByUserId: userA,
      participants: {
        createMany: {
          data: [
            { userId: userA },
            { userId: userB }
          ]
        }
      }
    }
  });
}

export async function sendMessage(conversationId: string, senderUserId: string, body: string) {
  const message = await db.message.create({
    data: {
      conversationId,
      senderUserId,
      body
    },
    include: {
      sender: true
    }
  });
  await db.conversation.update({ where: { id: conversationId }, data: { updatedAt: new Date() } });
  return message;
}

export async function listConversationsForUser(userId: string) {
  return db.conversation.findMany({
    where: {
      participants: {
        some: { userId, isBanned: false }
      }
    },
    include: {
      participants: true,
      messages: {
        take: 1,
        orderBy: { createdAt: "desc" }
      }
    },
    orderBy: {
      updatedAt: "desc"
    }
  });
}
