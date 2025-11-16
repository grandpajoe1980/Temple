# Temple (Next.js Edition)

Temple is a multi-tenant platform for congregations to manage public identity, share events and media, and host rich community features. This repository contains the new Next.js + Prisma implementation described in the product specification.

## Stack
- Next.js 14 (App Router) with TypeScript
- Prisma ORM + SQLite (development)
- Tailwind CSS with custom warm palette
- NextAuth.js (credentials provider) for email/password login
- Transactional email abstraction (console-based for dev)

## Getting started
1. Install dependencies
   ```bash
   npm install
   ```
2. Create your environment file
   ```bash
   cp .env.example .env
   ```
3. Apply the database schema
   ```bash
   npx prisma migrate dev
   ```
4. Run the development server
   ```bash
   npm run dev
   ```

Open http://localhost:3000 to access the landing page.

## Project structure
```
app/                 # App Router routes (landing, auth, explore, tenant areas, admin, messages)
components/          # Reusable UI, layout, form, and feature components
lib/                 # Database client, auth helpers, permissions, notifications, utilities
services/            # Feature services (email, tenants, messaging, notifications, helpers)
prisma/              # Prisma schema and migrations
```

## Core features implemented
- Email/password registration, login, forgot/reset password flows (NextAuth + bcrypt hashing)
- Multi-tenant data model covering tenants, memberships, posts, media, events, messaging, notifications, and settings
- Landing page search experience and `/explore` results respecting public tenants
- Tenant shell at `/t/[tenantSlug]` with home, calendar, posts, sermons, podcasts, books, members, chat, and control panel tabs
- Basic Control Panel with general + privacy forms (extensible for branding, features, permissions)
- Messaging hub (`/messages`) with DM list placeholder wired to Prisma service
- Admin dashboard for super admins to review tenants/users

## Development notes
- Email sending is abstracted under `services/emailService.ts` and currently logs payloads. Swap the implementation for real providers when deploying.
- Permission helpers live in `lib/permissions.ts` and should be used for any new server or client checks.
- Tenant creation, onboarding, and conversation scaffolding live in `services/tenantService.ts`.
- Notifications use `lib/notifications.ts` and `services/notificationService.ts`; extend as new triggers are added.

## Scripts
- `npm run dev` – start Next.js in development
- `npm run build` – production build
- `npm run start` – start production server
- `npm run lint` – lint via Next.js

## Testing assumptions
Automated tests are not yet included. Sanity check flows via manual testing after running migrations. Future work should include unit tests for services and integration tests for major flows.
