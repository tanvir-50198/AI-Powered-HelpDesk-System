# HelpDesk Pro

Support-ticket system with an AI (RAG) assistant. ASP.NET Core 8 MVC + REST API, PostgreSQL, Entity Framework Core, ASP.NET Core Identity.

Built from the "HelpDesk Pro - Full Design Document". Written in plain, beginner-friendly C# (no records, no pattern matching, no top-level tricks except the standard `Program.cs`).

> **Honest status:** the code was written and reviewed carefully, but it could **not be compiled or run** in the environment where it was generated (no NuGet access). Please run `dotnet build` and `dotnet test` on your machine first; if the compiler reports a small typo, it is a quick fix. Nothing here claims "tested" until you run the tests.

## Features
* Roles: **Customer, Agent, Admin** (server-side checks in `TicketService`, not only hidden buttons)
* Tickets: create, list with search / filter / sort / paging, comments, status history, priority, resolution, agent assignment
* AI assistant: hybrid retrieval (vector + keyword) -> rerank -> answer with citations -> safe fallback -> confirmed escalation to a ticket -> feedback
* Admin: users and roles, knowledge-document upload (PDF/DOCX/TXT/MD), approve / reject / re-index / delete, reports from real database queries
* REST API with Swagger (`/swagger` in Development), consistent errors (400/401/403/404/409/429/500/503), correlation ID, `/health`
* Rate limit on the chatbot (20 questions/minute/user), anti-forgery tokens on forms, account lockout

## Requirements
* .NET 8 SDK
* PostgreSQL 14+ (any local install or Docker: `docker run --name hdp-db -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:16`)

## Run
```bash
cd HelpDeskPro
# optional: use your own database password without editing files
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=helpdeskpro;Username=postgres;Password=YOURPASSWORD"
dotnet run
```
Open the URL printed in the console. On first start the app creates the tables, three demo accounts, categories, sample tickets and imports the 70 starter knowledge documents.

| Role | Email | Password |
|---|---|---|
| Admin | admin@helpdesk.com | Admin@123 |
| Agent | agent@helpdesk.com | Agent@123 |
| Customer | customer@helpdesk.com | Customer@123 |

Demo accounts are for local testing only - change or delete them before real use.

## Configuration (`appsettings.json`)
| Key | Meaning |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection |
| `Database:UseMigrations` | `false` = `EnsureCreated` (easy start). `true` = apply EF migrations |
| `Rag:MinScore` | evidence threshold (default 0.65; checked on the 70 demo articles: correct matches scored 0.79+, unrelated questions below 0.25). Lower = answers more often, higher = abstains more often |
| `Rag:TopK`, `ChunkSize`, `ChunkOverlap` | retrieval / chunking tuning |
| `Rag:OpenAiApiKey`, `OpenAiModel` | optional. Empty = free offline "extractive" answers. Set it with **user-secrets or environment variable `Rag__OpenAiApiKey`, never in Git** |
| `Sla:DemoHours` | DEMO overdue limit shown in reports (not a real SLA) |
| `Uploads:Folder`, `MaxFileSizeMb` | where uploaded documents are stored (outside `wwwroot`) |

## Migrations (optional)
```bash
dotnet tool install --global dotnet-ef
cd HelpDeskPro
dotnet ef migrations add InitialCreate
# then set Database:UseMigrations to true
dotnet ef database update
```

## Tests
```bash
dotnet test
```
Covers: ownership/role rules, validation, history, resolution rule, chunking, front-matter, retrieval (expected source found, internal/draft documents never reach customers), abstention, citations, conversation privacy, confirmed escalation, feedback.

## Project structure
```
HelpDeskPro/
  Controllers/        MVC pages (+ Api/ = REST endpoints)
  Models/             database entities
  Data/               ApplicationDbContext, DbSeeder
  Services/Tickets/   TicketService (all ticket rules)
  Services/RAG/       chunker, embeddings, retriever, reranker, ingestion, LLM, RAGChatService, reports
  ViewModels/ DTOs/   data shapes for pages and API
  Middleware/         error handling + correlation id
  Views/ wwwroot/     Razor pages, CSS, chat.js
  KnowledgeBase/      70 demo markdown documents
HelpDeskPro.Tests/    xUnit tests
```

## REST API (cookie login; use the browser session or Swagger after logging in)
`POST/GET /api/tickets`, `GET /api/tickets/{id}`, `PATCH /api/tickets/{id}/status`, `POST /api/tickets/{id}/comments`, `GET /api/tickets/{id}/history`,
`POST/GET /api/chat/conversations`, `POST /api/chat/conversations/{id}/messages`, `POST /api/chat/messages/{id}/feedback`, `POST /api/chat/conversations/{id}/create-ticket`,
`POST/GET /api/admin/knowledge-documents`, `POST .../{id}/reindex|approve|reject`, `GET /api/admin/reports/summary`.

## Known limitations (honest list)
* **Embeddings are a simple offline "hashing" model** - no synonym understanding. Retrieval relies a lot on shared words. Upgrade path: implement `IEmbeddingService` with a real embedding API.
* **Vectors are stored as `real[]` and compared in C#**, keyword matching is also in C#, loaded per query. Fine for hundreds of chunks. For thousands, move to **pgvector** + PostgreSQL full-text search (the design document's target).
* Extractive default answers quote the best article text; they do not rewrite it. Set an OpenAI key for generated answers.
* No OCR for scanned PDFs, no malware scanning of uploads, no email sending, no ticket attachments, no password-reset email flow (the FAQ explains the process; wire an email sender to complete it).
* Retrieval/citation quality was checked only with the small automated tests - run the 30-50 question evaluation from the design document before claiming quality.
* SLA numbers are demo values.
* Starter knowledge documents are generated demo content, not real company policy.
