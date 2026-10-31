# HelpDeskPro — ASP.NET Core + React

This folder combines the supplied ASP.NET Core MVC/API project with the React SPA frontend.

## Structure

- `HelpDeskPro/` — ASP.NET Core 8 application and test project
- `helpdeskpro-web/` — React 18 + TypeScript + Vite frontend
- The SPA API controllers and DTOs have been copied into the ASP.NET project:
  - `HelpDeskPro/Controllers/Api/AuthApiController.cs`
  - `HelpDeskPro/Controllers/Api/LookupsApiController.cs`
  - `HelpDeskPro/Controllers/Api/TicketsManagementApiController.cs`
  - `HelpDeskPro/Controllers/Api/UsersApiController.cs`
  - `HelpDeskPro/Controllers/Api/ChatConversationApiController.cs`
  - `HelpDeskPro/Controllers/Api/KnowledgeAdminExtrasApiController.cs`
  - `HelpDeskPro/DTOs/SpaDtos.cs`

## Requirements

- .NET 8 SDK
- Node.js 20+ and npm
- PostgreSQL (for the real backend)

## Configure PostgreSQL

Update `HelpDeskPro/appsettings.json` or preferably use .NET User Secrets / environment variables for:
- `ConnectionStrings:DefaultConnection`
- `Rag:OpenAiApiKey` (optional; the app has an offline extractive answer mode when no key is configured)

The included connection string is a local development placeholder. Do not commit real credentials or API keys.

## Run the backend

From this directory:

```bash
cd HelpDeskPro
dotnet restore
dotnet build
dotnet run
```

The app uses the launch profile in `HelpDeskPro/Properties/launchSettings.json`. The React Vite proxy default is `http://localhost:5080`; if your selected backend profile uses another port, update `helpdeskpro-web/.env` using `.env.example` as the template.

## Run the React frontend

In a second terminal:

```bash
cd helpdeskpro-web
npm install
copy .env.example .env
npm run dev
```

Open the Vite URL shown in the terminal (normally `http://localhost:5173`).

For a frontend-only demo without PostgreSQL/backend:

```bash
cd helpdeskpro-web
npm install
npm run dev:mock
```

## Production build

```bash
cd helpdeskpro-web
npm install
npm run build
```

The output is `helpdeskpro-web/dist/`. For same-origin cookie authentication, host the SPA and proxy `/api` to ASP.NET, or follow the `/app` hosting notes in `helpdeskpro-web/README.md`.

## Important verification status

The source files have been combined, but this environment did not have a usable .NET SDK, so the ASP.NET project and the newly added C# controllers could not be compiled here. The frontend build was attempted, but dependency installation did not complete in this environment; TypeScript therefore reported missing `node` and `vite/client` type definitions. Run the commands above on a machine with .NET 8 and npm registry access, then address any build output before deployment. The original backend test project is included but has not been executed here.
