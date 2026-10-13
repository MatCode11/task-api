# TaskApi — Plan

## Goal
A small REST API for managing tasks. It is a backend learning project: each milestone teaches one real-world ASP.NET Core skill (persistence, testing, validation, auth, and so on) on top of the CRUD API that already exists.

## Assumptions
- The main goal is learning, not shipping to users. Milestones are ordered by "what a backend developer needs next", and each one stays small enough to finish in a sitting or two.
- One developer, working locally on macOS. Both .NET 9 and .NET 10 SDKs are installed.
- .NET 9 (STS) reaches end of support on **2026-11-10**. .NET 10 is the current LTS (supported until Nov 2028), so the project moves to net10.0 first.
- SQLite is the first real database because it needs no server or Docker. PostgreSQL is a "Later" item.
- Auth uses ASP.NET Core Identity's built-in API endpoints (bearer tokens). This avoids hand-rolling JWT code on the first attempt. Hand-written JWT can be a later exercise.
- The API keeps its current shape: controllers, then the service layer, then EF Core. No repository layer, MediatR, or CQRS. They add ceremony without teaching much at this size.
- No frontend.

## Scope
**MVP (the learning roadmap, M1–M7):**
- .NET 10 upgrade and a src/tests folder layout
- SQLite with EF Core migrations, so data survives restarts
- Automated tests (unit and integration)
- Input validation and consistent ProblemDetails error responses
- Pagination, filtering, and sorting on `GET /api/tasks`
- Structured logging and a health check
- User accounts, with tasks owned by the logged-in user

**Later:** PATCH endpoint, Docker with PostgreSQL, GitHub Actions CI, FluentValidation, Serilog, rate limiting, API versioning, output caching, hand-written JWT auth, soft delete, due dates and priorities on tasks.

**Not doing:** frontend, microservices, message queues, cloud deployment, repository/unit-of-work abstractions.

## Stack
| Part | Choice | Why |
|---|---|---|
| Runtime | .NET 10 (LTS), ASP.NET Core controllers | Current LTS. .NET 9 support ends 2026-11-10 |
| Data access | EF Core 10 | Already in use |
| Database | SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) | Real file-based DB with real migrations and zero setup |
| Migrations | `dotnet-ef` CLI tool | Standard EF workflow |
| API docs | Built-in `Microsoft.AspNetCore.OpenApi` (+ optional Scalar UI) | Already in use. Scalar gives a browser UI for the OpenAPI doc |
| Validation | DataAnnotations on DTOs + `[ApiController]` automatic 400s | Built in, nothing new to learn first |
| Errors | `AddProblemDetails()` + `IExceptionHandler` | Built-in RFC 9457 error format |
| Logging | Built-in `ILogger<T>` | Built in. Serilog later |
| Tests | xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) | The documented approach for ASP.NET Core integration tests |
| Auth | ASP.NET Core Identity API endpoints (`AddIdentityApiEndpoints`, `MapIdentityApi`) | Built-in register/login/bearer tokens |

Confirm exact package versions with `dotnet add package` (it picks the latest stable version) when each milestone starts. Keep all EF Core and ASP.NET packages on the same major version.

## Structure
Target layout after M1:
```
TaskApi.sln
global.json                 # pins SDK 10.x
src/TaskApi/                # the web API (moved from the repo root)
  Controllers/              # HTTP layer only: routing, status codes
  Services/                 # business logic (ITaskService/TaskService)
  Data/                     # AppDbContext, Migrations/ (from M2)
  Models/                   # EF entities (TaskItem, later AppUser)
  Dtos/                     # request/response records
  Errors/                   # exception handler(s) (from M4)
  Program.cs
  TaskApi.http              # manual requests
tests/TaskApi.Tests/        # xUnit: Unit/ and Integration/ folders
README.md
PLAN.md
CLAUDE.md
docs/features/              # feature specs (feature-designer)
```
Why move to `src/`: the web project currently sits at the repo root. By default a .csproj compiles every `.cs` file in its folder and all subfolders, so a `tests/` folder under the root would get compiled into the API.

## Data model (sketch)
- **TaskItem**: Id, Title (required, max 200), Description (max 2000), IsCompleted, CreatedAt, UpdatedAt. M7 adds UserId (FK to the user, required).
- **AppUser** (M7): the Identity user. One user has many TaskItems.
- Index on (UserId, IsCompleted) in M7 for filtered lists.

## Milestones
- [ ] **M1 — Upgrade and tidy:** project targets net10.0 and EF Core 10, lives in `src/TaskApi`, `global.json` pins SDK 10, README has run instructions. Demo: `dotnet run --project src/TaskApi`, then all requests in `TaskApi.http` still work.
- [ ] **M2 — Real database:** swap InMemory for SQLite, add the first migration (`InitialCreate`), and move seeding out of `EnsureCreated` so migrations own the schema. Connection string goes in `appsettings.json`. Demo: create a task, restart the app, and the task is still there. `dotnet ef migrations list` shows the migration.
- [ ] **M3 — Tests:** add `tests/TaskApi.Tests`. Unit tests for `TaskService` use a SQLite in-memory connection. Integration tests cover each endpoint through `WebApplicationFactory` (happy path, 404, current 400 on empty title). Demo: `dotnet test` is green.
- [ ] **M4 — Validation and errors:** DataAnnotations on the DTOs (required, max lengths matching the DB). Remove the manual title checks from the controller. All errors return ProblemDetails, including 404s and unhandled exceptions via `IExceptionHandler` (no stack traces outside Development). Demo: invalid POST returns a 400 ProblemDetails with field errors. Tests updated.
- [ ] **M5 — Pagination, filtering, sorting:** `GET /api/tasks?page=1&pageSize=20&isCompleted=false&search=api&sort=-createdAt` returns `{ items, page, pageSize, totalCount }`, with pageSize capped (e.g. 100). Demo: run it from `TaskApi.http`. Tests cover the edge cases.
- [ ] **M6 — Logging and health:** inject `ILogger<T>` into the service and log create/update/delete with structured properties (`{TaskId}`). Add HTTP request logging and a `/health` endpoint that checks the DB. Demo: the console shows structured logs and `/health` returns Healthy.
- [ ] **M7 — Auth and ownership:** Identity API endpoints (`/register`, `/login`). Task endpoints require auth, and a user only sees and edits their own tasks (another user's task returns 404). Migration adds the user tables and `UserId`. Demo: register two users in `TaskApi.http` and show that each sees only their own tasks. Tests cover auth.

## Features
(feature-designer adds links to specs here)

## Risks and open questions
- **.NET 9 end of support is 2026-11-10.** That is why the upgrade is M1.
- Moving the project into `src/` changes paths in `.vscode/launch.json` and `tasks.json`. Update them in M1.
- Seeding: decide in M2 between `HasData` (part of migrations) and a Development-only seeder at startup. The recommendation is a dev-only seeder, so tests start from an empty DB.
- SQLite limits: there is no real `DateTimeOffset` ordering and some ALTERs force a table rebuild. This is fine for learning. Revisit when moving to PostgreSQL.
- M7 changes every task query to be filtered by user. Doing M3 (tests) first makes that change safe.
- Open: does the user want Identity endpoints (recommended) or to hand-write JWT issuing as the learning exercise?
- Open: add Scalar for a browser UI over the OpenAPI doc, or keep using `TaskApi.http`?

## Progress log
- 2026-10-13: Plan created. Existing state: CRUD controller, async service layer with DTOs, EF Core InMemory with two seeded tasks, OpenAPI on net9.0.
