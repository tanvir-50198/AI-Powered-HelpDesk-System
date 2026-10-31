# HelpDesk Pro – React frontend

React 18 · TypeScript · Vite · Ant Design 5 · TanStack Query · React Router · Recharts

A frontend for the `HelpDeskPro` ASP.NET project: customers, agents and admins each get their own screens.
It talks to the REST API (`/api/...`) with the same Identity cookie as the MVC pages.

## 1. Add the missing API endpoints (once)

The existing API had no JSON login and a few missing actions. Copy the folder `backend-additions/` into
the ASP.NET project (keep the sub-folders):

| File | Adds |
|---|---|
| `DTOs/SpaDtos.cs` | request / response objects |
| `Controllers/Api/AuthApiController.cs` | `POST /api/auth/login`, `register`, `logout`, `GET me` |
| `Controllers/Api/LookupsApiController.cs` | `GET /api/lookups/categories`, `GET /api/dashboard/summary` |
| `Controllers/Api/TicketsManagementApiController.cs` | `PATCH /tickets/{id}/priority`, `PUT .../resolution`, `POST .../assign` |
| `Controllers/Api/UsersApiController.cs` | `GET /admin/users`, `GET /admin/agents`, `PATCH /admin/users/{id}/role` |
| `Controllers/Api/ChatConversationApiController.cs` | `GET /chat/conversations/{id}` (messages, sources, ratings) |
| `Controllers/Api/KnowledgeAdminExtrasApiController.cs` | `DELETE /admin/knowledge-documents/{id}`, `POST .../import-folder` |

No `Program.cs` change is needed for development. **These files were written without a .NET SDK available,
so build the project and fix anything the compiler reports.**

## 2. Run it

```bash
npm install
cp .env.example .env      # set VITE_API_PROXY_TARGET to your API (see launchSettings.json)
npm run dev               # http://localhost:5173
```

No backend handy? `npm run dev:mock` starts an in-memory mock API plus Vite
(demo logins are on the login page).

Demo accounts (from `DbSeeder.cs`): `customer@helpdesk.com`, `agent@helpdesk.com`, `admin@helpdesk.com`.
Set `VITE_SHOW_DEMO_ACCOUNTS=false` for anything real.

## 3. Production

```bash
npm run build             # output in dist/
```

**Option A – separate static host:** serve `dist/` and reverse-proxy `/api` to ASP.NET on the same origin
(keeps the cookie first-party; no CORS).

**Option B – inside the ASP.NET site under `/app`:**
```bash
VITE_BASE=/app/ npm run build      # copy dist/* to HelpDeskPro/wwwroot/app/
```
In `Program.cs`, after `app.UseAuthorization();` and before `app.MapControllerRoute(...)`:
```csharp
app.MapFallbackToFile("/app/{*path:nonfile}", "app/index.html");
```

## Project layout
```
src/api/        types.ts (mirrors the C# DTOs) · client.ts (fetch + ProblemDetails) · endpoints.ts · hooks.ts
src/auth/       session context + route guards (RequireAuth / RequireRole)
src/layouts/    AppLayout (role-aware nav, drawer on mobile) · AuthLayout
src/features/   auth · dashboard · tickets · chat · admin
src/components/ StatusTag, PriorityBadge, StatCard, PageHeader, states
src/theme.tsx   design tokens, light/dark
mock/server.mjs in-memory API for demos and tests
```

## Behaviour worth knowing
- Server rules are mirrored in the UI for fast feedback (resolution required before Resolved/Closed,
  only admins re-open closed tickets), but the server stays the authority.
- Chat conversations are created with the first question, so no empty chats pile up.
- Answers cite sources as `[1]`; these render as chips that map to the numbered Sources list.
- Role changes may need the user to sign in again before they take effect.
- Assistant answers are shown as plain text (not markdown).

## Scripts
`dev` · `dev:mock` · `build` · `typecheck` · `lint`
