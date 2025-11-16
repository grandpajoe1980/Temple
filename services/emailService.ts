export interface EmailPayload {
  to: string;
  subject: string;
  html: string;
  text: string;
}

interface EmailService {
  send(payload: EmailPayload): Promise<void>;
}

class ConsoleEmailService implements EmailService {
  async send(payload: EmailPayload) {
    if (process.env.NODE_ENV !== "production") {
      console.info("[email]", payload);
    }
  }
}

export const emailService: EmailService = new ConsoleEmailService();

export async function sendPasswordResetEmail(email: string, url: string) {
  await emailService.send({
    to: email,
    subject: "Temple password reset",
    text: `Reset your password: ${url}`,
    html: `<p>Reset your password:</p><p><a href="${url}">${url}</a></p>`
  });
}

export async function sendNewDmEmail(email: string, preview: string, url: string) {
  await emailService.send({
    to: email,
    subject: "You received a new message on Temple",
    text: `${preview}\n${url}`,
    html: `<p>${preview}</p><p><a href="${url}">Open conversation</a></p>`
  });
}

export async function sendAnnouncementEmail(email: string, title: string, url: string) {
  await emailService.send({
    to: email,
    subject: `New announcement: ${title}`,
    text: `${title}\n${url}`,
    html: `<p>${title}</p><p><a href="${url}">Read announcement</a></p>`
  });
}
