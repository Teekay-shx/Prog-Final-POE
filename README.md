# RaceDay — Parts 1–3

RaceDay manages running, walking and cycling events. This repository holds the database design (Part 1),
the ASP.NET Core Web API (Part 2) and the ASP.NET Core MVC web app that consumes it (Part 3).

**Demo video (unlisted YouTube):** _[paste your link here]_

**Green build:** _[paste a screenshot of the passing GitHub Actions run here]_

## Part 3 at a glance

| Piece | What it is |
|---|---|
| `src/RaceDay.Api` | Part 2 API. Part 3 adds Azure Blob Storage uploads (event banners, profile pictures), `GET /api/events/mine`, `GET /api/events/{id}/enrolments`, and results with finisher counts. |
| `src/RaceDay.Web` | MVC app. **No database access and no business logic**: every action calls the API through one typed client (`Services/RaceDayApi.cs`). |
| `src/RaceDay.Web/Dockerfile` | Multi-stage build, non-root user, port 8080. |
| `docker-compose.yml` | Optional one-command start for the web container. |
| `.github/workflows/dotnet-ci.yml` | Builds the API **and** the MVC project, runs tests, and checks the Docker image builds. |

### MVC structure

```
src/RaceDay.Web/
├── Controllers/     Account, Home, Events, Dashboard, Profile, MyEnrolments, MyResults,
│                    OrganiserEvents, OrganiserCategories, OrganiserEnrolments, OrganiserResults
├── Services/        IRaceDayApi / RaceDayApi (typed HttpClient), ApiException
├── Infrastructure/  ApiExceptionFilter (friendly errors), UserSession, UiHelpers (status colours)
├── Models/          Api/ (DTOs matching the API) and ViewModels/
├── Views/           Razor views, one folder per controller + Shared (_Layout, _EventCard, _Alerts)
└── wwwroot/         site.css (RaceDay theme), site.js, placeholder banner
```

### How login and sessions work

1. The user logs in on the MVC site. The MVC app calls `POST /api/auth/login`.
2. The API replies with its session cookie. The MVC app keeps that cookie **server-side in ASP.NET Session**
   and signs the user in to its own cookie (`RaceDay.Web.Auth`) carrying their id, name and role.
3. Every later call to the API replays the stored cookie, so the API still enforces roles and ownership.
4. `[Authorize(Roles=...)]` on the MVC controllers gives each role its own pages and navbar.
   Anonymous users go to the login page; the wrong role gets an Access Denied page. Logout clears both.

If the API rejects a request the user sees a friendly message (or the login page on 401), never a stack trace.

### Azure Blob Storage (API only)

The MVC app never talks to Azure. It posts the file to the API (`POST /api/events/{id}/banner`,
`POST /api/users/me/profile-picture`); the API validates it (JPEG/PNG/WebP/GIF, max 5 MB), stores it in
the `raceday-images` container, saves the blob URL on the event or user, and the MVC app simply shows that URL.

1. In the Azure portal create a **Storage account** (Standard, LRS). Allow anonymous blob access on the account
   (the container is created with *Blob* public read so images can be shown in the browser).
2. Copy the connection string (Access keys). **Do not commit it.** Store it with user-secrets:

```bash
cd src/RaceDay.Api
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your connection string>"
```

If it is not configured the upload endpoints return 503 and the site shows a friendly message; everything else still works.

### Database changes in Part 3

`Events.BannerImageUrl` and `Users.ProfilePictureUrl` (both `NVARCHAR(500) NULL`) were added. The ERD
(`docs/raceday_erd.png`), `docs/raceday_schema.sql` and `docs/api-endpoint-plan.md` have been updated.
If you already created `InitialCreate` before these columns existed, add a second migration:

```bash
dotnet ef migrations add AddImageUrls --project src/RaceDay.Api --startup-project src/RaceDay.Api
```

Otherwise one `InitialCreate` covers everything. The API applies migrations on startup.

## Running it

### 1. Start the API on your machine (it uses your SQL Server)

Run it from Visual Studio, or:

```bash
dotnet run --project src/RaceDay.Api --launch-profile http-docker
```

The `http-docker` profile listens on `0.0.0.0:5249` so the Docker container can reach it. Seeded logins
(password `Password123!`): `naledi.khumalo@raceday.co.za` (Organiser), `thabo.nkosi@example.com` (Participant).

### 2. Run the MVC app locally (no Docker)

```bash
dotnet run --project src/RaceDay.Web
```

Open http://localhost:5250 (API address is `Api:BaseUrl` in `appsettings.json`, default `http://localhost:5249`).

### 3. Run the MVC app in Docker (single command)

```bash
docker build -t raceday-web ./src/RaceDay.Web && docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5249 --add-host=host.docker.internal:host-gateway --name raceday-web raceday-web
```

Open http://localhost:8080. Or with Compose: `docker compose up --build`.

The container uses the same code and the same API, so behaviour matches the local run. The only difference is
the API address, passed in with `Api__BaseUrl` (`host.docker.internal` = your machine, where the API runs).

**Dockerfile practices used:** multi-stage build (SDK image to build, slim ASP.NET runtime image to run);
`.csproj` copied first so the restore layer is cached; non-root `app` user; fixed port 8080; configuration
through environment variables; `.dockerignore` keeps `bin/`, `obj/` and local settings out of the image.

## Features checklist

- **Organiser:** dashboard (events, enrolments per event, upcoming dates); create/edit/delete events with banner upload;
  categories per event; enrolments per event with category and status; results entry and publish.
- **Participant:** home page with upcoming events and filters (search, type, location, include past); event details
  with banner and categories; enrol in one click; my enrolments with status; my results ("47th out of 312 finishers");
  profile edit and picture upload.
- **Status colours:** Registered = amber, Checked in = green, Cancelled = red.
- **Validation:** client and server side; API errors are shown as friendly messages.

## CI/CD

On every push and pull request the workflow restores, builds the API, builds the MVC project, builds the solution,
runs all tests, and then builds the MVC Docker image.

## Tests

```bash
dotnet test RaceDay.sln
```

`Part3FeatureTests` covers banner and profile-picture upload (owner, other organiser, participant, anonymous, bad file),
the organiser dashboard data, the per-event roster, delete-with-enrolments conflict, and finisher counts.
Azure is replaced by a fake in tests.

## Submission checklist

- [ ] `dotnet ef migrations add ...` and commit `Migrations/`
- [ ] `dotnet build` and `dotnet test` pass locally
- [ ] Azure connection string set in user-secrets (never committed)
- [ ] Docker command above works with the API running
- [ ] 20+ commits in a branch or folder for Part 3
- [ ] Green Actions screenshot and YouTube link pasted at the top of this README

---

# Part 2 — RESTful API (reference)

This is the ASP.NET Core Web API that implements the plan submitted in Part 1
(`/docs/raceday_erd.png`, `/docs/api-endpoint-plan.md`, `/docs/raceday_schema.sql`). It covers
authentication, user profiles, events, categories, enrolments, results, and payments, built
Code-First with Entity Framework Core against SQL Server.

## Project structure

```
RaceDay/
├── RaceDay.sln
├── src/
│   └── RaceDay.Api/
│       ├── Controllers/      7 controllers — Auth, Users, Events, Categories, Enrolments, Results, Payments
│       ├── Models/            EF Core entities + status "enum" classes (matches the Part 1 ERD)
│       ├── DTOs/               Request/response shapes, grouped by resource
│       ├── Data/                RaceDayDbContext (Fluent API constraints) + DbSeeder
│       ├── Services/            ICurrentUserService — reads the logged-in user off the session cookie
│       ├── Program.cs           DI, EF Core, cookie auth, Swagger, migration-on-startup
│       └── appsettings*.json
├── tests/
│   └── RaceDay.Api.Tests/      xUnit + WebApplicationFactory integration tests (EF Core InMemory)
└── .github/workflows/
    └── dotnet-ci.yml           Builds + runs all tests on every push/PR
```

## Before you run it: generate the EF Core migration

This solution was written in a sandboxed environment with no internet access to NuGet, so the
code below could be carefully written and reviewed line-by-line, but **could not actually be
compiled or run here** — `dotnet restore` fails outright (no route to `api.nuget.org`). You must
do the first build, the migration, and the test run yourself, in Visual Studio or the CLI, on a
machine with normal internet access. This also means one thing is deliberately **not** included:
the EF Core Migrations folder, since scaffolding a migration requires compiling the project first.

Once you can restore packages, generate it with:

```bash
cd RaceDay
dotnet tool install --global dotnet-ef   # if you don't already have it
dotnet ef migrations add InitialCreate --project src/RaceDay.Api --startup-project src/RaceDay.Api
```

This produces a `Migrations/` folder inside `src/RaceDay.Api`. Commit it — the schema it generates
must match `/docs/raceday_schema.sql` (it does, by design: every model, `[Column(TypeName=...)]`
attribute, and Fluent API check constraint in `Data/RaceDayDbContext.cs` was written to mirror that
script table-for-table).

## Running it

1. Point `appsettings.json` / `appsettings.Development.json` at your SQL Server instance (defaults
   to LocalDB in Development).
2. `dotnet ef migrations add InitialCreate ...` (above), if you haven't already.
3. `dotnet run --project src/RaceDay.Api`. On startup the app runs `Database.Migrate()` and then
   seeds the same sample data as the Part 1 SQL script (2 organisers, 2 participants, 3 events,
   5 categories, 4 enrolments, 1 result, 4 payments) — but only if `Users` is empty, so it's safe
   to restart the app without duplicating data.
4. Open `/swagger` (the launch profile does this automatically). Every endpoint is listed there
   with its purpose, request body, and response codes, pulled from the XML doc comments on each
   controller action.

Seeded login for trying things out in Swagger: `naledi.khumalo@raceday.co.za` (Organiser) or
`thabo.nkosi@example.com` (Participant), password `Password123!` for all seeded users.

## How authentication and roles work

The brief asks for "session management to maintain the user's authenticated state and role for
all subsequent requests" rather than a stateless token scheme, so this uses **cookie
authentication** (`Microsoft.AspNetCore.Authentication.Cookies`): `/api/auth/login` and
`/api/auth/register` issue an HttpOnly session cookie carrying the user's id and role as claims;
every subsequent request on the same client sends that cookie back automatically, and ASP.NET
Core's authentication middleware re-establishes `HttpContext.User` from it on every request — that
*is* server-recognised session state, just implemented through the cookie rather than
`HttpContext.Session`'s in-memory dictionary.

- `[Authorize]` on a controller/action requires any logged-in user; a missing/invalid cookie
  returns **401 Unauthorized** (configured explicitly in `Program.cs`, since this is an API with no
  login page to redirect to).
- `[Authorize(Roles = "Organiser")]` / `[Authorize(Roles = "Participant")]` / `[Authorize(Roles =
  "Admin")]` restrict an endpoint to one role; a logged-in user of the wrong role gets
  **403 Forbidden**.
- Beyond role, several endpoints also check **ownership** in the handler itself — e.g. an
  Organiser can only edit/delete their *own* events, categories and results, and a Participant can
  only cancel their *own* enrolment. These checks live in the controllers (`ICurrentUserService`
  exposes the caller's id/role) since role-based attributes alone can't express "owns this row."
- Passwords are hashed with ASP.NET Core Identity's `PasswordHasher<User>` (PBKDF2 under the hood)
  — the plaintext password is never stored or logged.

## How the implementation maps to the Part 1 plan

Every route in `/docs/api-endpoint-plan.md` is implemented as planned, with one explained addition:

**`Events.Distance` and `Events.Type`** — Part 2's functional requirements state "each event must
capture a name, description, date, location, distance, and event type (run, walk, or cycle)",
which the Part 1 ERD didn't include (distance only existed per-category there). Rather than let the
implementation silently drift from the submitted plan, Part 1's ERD, SQL script and endpoint plan
have been updated to add these two columns to `Events`, with this note added as the explanation the
brief asks for. No other table changed — Payments, Results, Enrolments, Categories and Users are
unchanged from Part 1.

**Payments** was already in the Part 1 plan as an addition beyond the required minimum (needed
because `Categories.EntryFee` implies something has to track whether that fee was paid), and is
implemented here the same way: `POST`/`GET /api/enrolments/{id}/payments`.

## Tests

`tests/RaceDay.Api.Tests` runs real HTTP requests against the full pipeline (controllers, cookie
auth, authorization, EF Core) via `WebApplicationFactory<Program>`, swapping SQL Server for the EF
Core InMemory provider so no database is needed to run them. Covers, with both success and failure
cases:

| File | Covers |
|---|---|
| `AuthTests.cs` | Registration (valid, duplicate email, disallowed self-registered role), login (correct/wrong password, unknown email), accessing a protected route while logged out |
| `RoleAccessTests.cs` | Every role boundary rejected: Participant creating an event, anonymous creating an event, Organiser enrolling in their own category, non-Admins listing all users, Participant recording a result |
| `EventManagementTests.cs` | Organiser create/update/delete on their own event, a different organiser blocked from editing it, public read access, invalid event type rejected |
| `EnrolmentsTests.cs` | Enrol → bib number assigned, shows up in "my enrolments", duplicate enrolment rejected, category-full rejected, non-existent category rejected, organiser roster view, cancel by owner vs. by a different participant |
| `UserProfileAndResultsTests.cs` | Profile update persists, result capture shows on the public leaderboard, duplicate result rejected, a participant's own results are scoped to them |

Run them with:

```bash
dotnet test RaceDay.sln
```

## CI/CD

`.github/workflows/dotnet-ci.yml` runs on every push and pull request to any branch: restores,
builds in Release, runs the full test suite, and uploads the `.trx` test results as a build
artifact. Once you've pushed this to GitHub, **add a screenshot of the green Actions run here**
(Actions tab → the workflow run → the green checkmark), since it couldn't be captured from this
sandboxed environment:

> _[Insert screenshot of a successful GitHub Actions run here once pushed]_

## Known gaps to close before submitting

- [ ] Run `dotnet ef migrations add InitialCreate` and commit the generated `Migrations/` folder
      (see above — not included for the reason explained there).
- [ ] Confirm `dotnet build` and `dotnet test` pass locally — they were reviewed file-by-file but
      never compiled, since this sandbox has no NuGet access.
- [ ] Push with a minimum of 20 meaningful commits on your own GitHub account, in a clearly named
      branch or subfolder for Part 2, per the brief's Section 6.
- [ ] Paste the green-build screenshot into this README once CI has run on GitHub.
