# Modernization Plan: BookCatalog .NET 10 Modernization

**Project**: BookCatalog (shared-legacy-app)

> **Authority.** This plan is a technical proposal subordinate to the approved
> specification `specs/dotnet-bookcatalog-modernization.md` (decisions D1-D9).
> If the two disagree, the specification wins. If a step cannot be done
> without weakening or reinterpreting an acceptance criterion, stop and ask a
> human. This plan is not executed by its authoring; implementation needs a
> separate go-ahead.

---

## Technical Framework

- **Language**: C# (.NET Framework 4.8)
- **Framework**: ASP.NET MVC 5, `System.Web`, `Global.asax` startup, `RouteConfig`/`FilterConfig`
- **Build Tool**: MSBuild (`nuget restore` + `packages.config`)
- **Database**: SQL Server LocalDB via EF6 `DbContext` with `DropCreateDatabaseIfModelChanges`
- **Key Dependencies**: Entity Framework 6, `packages.config`-managed NuGet packages (Newtonsoft.Json 13.0.3 is a classic-only binding redirect)

---

## Overview

Move BookCatalog from ASP.NET MVC 5 / .NET Framework 4.8 / EF6 to ASP.NET
Core MVC on .NET 10 with EF Core and the SQL Server provider (D1, D2), in
five small, ordered, individually verifiable tasks instead of one big-bang
upgrade. Everything is local-only: no Azure, no deployment (D8, NFR-5).

| # | Task | Outcome |
|---|------|---------|
| 1 | `001-sdk-style-net10-build` | SDK-style `net10.0` web project; `dotnet build` is green. App does not yet serve the catalog. |
| 2 | `002-aspnetcore-mvc-migration` | Hosting, routing, controllers, views, static content, validation, antiforgery, 404 behavior on ASP.NET Core. |
| 3 | `003-efcore-di-seed-migrations` | EF Core + DI, `BookCatalogModernizedLab`, migrations at startup, seed-if-empty. EF6 and classic files gone. |
| 4 | `004-tests-and-local-crud-verification` | New `tests/` project; AC-1 to AC-8 verified; local CRUD check. |
| 5 | `005-dependency-cve-scan` | CVE scan of the NuGet graph and bounded remediation. |

Ordering rationale: each task leaves `dotnet build BookCatalog.sln -c Release`
green so a failure is attributable to one task and one rollback unit.

### Sequencing notes (known seams)

- **Task 1 is a build-only milestone.** To get a green build while classic
  code still exists, the not-yet-migrated classic files (controllers, models,
  `Views/**`, `Global.asax*`, `App_Start/**`) are excluded from compilation
  with explicit project-file item removals rather than deleted. Those
  exclusions must all be gone by the end of task 3.
- **Task 2 precedes EF Core.** Controllers need a context type, so task 2 may
  add only the EF Core package reference and a compile-only
  `ApplicationDbContext` (`DbSet<Book>`, no migrations, no seed, no database
  creation) registered in DI. Task 2 is validated without database access;
  runtime CRUD is proven in tasks 3 and 4.
- **Classic artifacts are deleted when migrated**, within allowed paths only
  (spec "Cleanup obligations").

---

## Behavior Preservation Rules (apply to tasks 2-4)

These restate spec FR/AC items so the plan cannot weaken them.

- **Routing (FR-1):** default route `{controller=Books}/{action=Index}/{id?}`; URL shapes unchanged.
- **List (FR-2):** active books only, ordered by title.
- **404 (FR-3):** Details, Edit (GET and POST) and Delete (GET) return HTTP 404 for an unknown id. Delete POST for a missing id redirects to the list (FR-7).
- **Validation (FR-4):** Title required max 200; Author required max 100; ISBN optional max 13; Published Year optional 1800-2100. Declared on the model (NFR-4). With nullable reference types enabled, `ISBN` and `PublishedYear` must stay optional (`string?`, `int?`) and `Title`/`Author` required; verify Core does not add implicit "required" to ISBN. Invalid input redisplays the form and saves nothing. Server-side validation is authoritative.
- **Defaults (FR-5):** new `Book` has `IsActive = true` and `CreatedDate = DateTime.Now` (local time, not UTC; decision Q5).
- **Edit (FR-6):** updates only Title, Author, ISBN, PublishedYear, IsActive; `CreatedDate` unchanged. Bind only those fields (no overposting of `CreatedDate`).
- **Antiforgery (FR-8):** explicit `[ValidateAntiForgeryToken]` (or an equivalent global auto-validate filter) on Create, Edit, and Delete POST; forms emit tokens.
- **Seed (FR-9, FR-12):** exactly the seven existing synthetic books (six active, 'The Matrix: The Shooting Script' inactive) with their existing fixed `CreatedDate` values, via startup logic only when the `Books` table is empty.
- **Visible UI (FR-13):** preserve labels, columns, layout, `Content/Site.css` (keep the `/Content/Site.css?v=2` URL working), the `YOUR BROWSER` box (rendered HTML-encoded, only when a user agent is present), visitor counter `001337`, static "Page generated in 0.042 seconds", and the Internet Explorer hint. Only the footer text naming MVC 5, EF 6, or .NET Framework 4.8 changes. Index's "No books found." message is kept.
- **Config (FR-14, FR-15):** no `debug="true"` / `customErrors Off` equivalent; developer exception page only in Development; no `Directory.CreateDirectory`, no `App_Data`.

## Security, Data, and Boundary Review

- **Data:** only the seven synthetic seed books or visibly synthetic test records. No import from the old MDF (D4, FR-16).
- **Connection settings (D7, NFR-1, NFR-2):** LocalDB, `Integrated Security=True`, `Initial Catalog=BookCatalogModernizedLab`, no `AttachDbFilename`, no user id or password, no secrets. Never point at a business database; never create an empty MDF.
- **No new auth (Exclusions):** no login, no authorization attributes. The Report's "Windows authentication" finding reflects the connection string only.
- **Output encoding / errors:** Razor encodes by default; do not use `Html.Raw` on the user-agent value or user input. Detailed errors only in Development (FR-14).
- **Test isolation:** tests use their own uniquely named, disposable LocalDB database (never `BookCatalogModernizedLab`) and drop it afterwards.
- **No deployment (D8):** no Key Vault, Bicep/infrastructure, publish, cleanup-of-Azure, containers, or cloud resources, and nothing under `examples/azure/`. No Azure packages.
- **Not added (D9 and request):** no System.Web adapters, YARP, side-by-side host, shared legacy schema, passwords, CI workflow changes, Java, or editor-specific work.
- **Dependencies:** only packages needed for FR-10 to FR-12 plus test-project packages; every added package (including the EF Core design-time package for migrations) is recorded in the pull request (D6).

## Common Rules For Every Task

- **Working directory:** `legacy-src/dotnet-bookcatalog/shared-legacy-app` (the "source root").
- **Allowed paths** (spec Constraints), relative to the source root: `src/BookCatalog.Web/**`, `BookCatalog.sln`, `global.json`, `NuGet.Config`, `README.md`, `tests/**`. Never edit `packages/`, `bin/`, `obj/`, generated database files, or an empty MDF.
- **Scope gate after every task:** `git diff --name-only` and `git status --short` must list only allowed paths (plus these two plan artifacts, see Open Items).
- **Rollback / containment (every task):** keep each task as one reviewable diff. To roll back, restore tracked files in that task's allowed paths to the last accepted state (for example `git restore -- <task paths>`) and delete only the untracked files that task created (identify them with `git status --short`; do not run a blanket clean). Drop any temporary LocalDB database. Do not start the next task until the current one is accepted.
- **Universal stop conditions** (spec "Stop conditions"): evidence contradicts the spec; a new question could change behavior, scope, or validation; a criterion needs a path outside allowed paths; validation fails for a reason caused by the change that cannot be fixed within the task's scope; a prerequisite (.NET 10 SDK, LocalDB, Windows tooling) is missing, in which case record it rather than report a pass; a secret, credential, business database, Azure resource, deployment or cost would be needed; a schema, public route, or dependency change beyond FR-10 to FR-12 is required.

---

## Tasks

### 001 - SDK-style net10.0 web project and green `dotnet build`

- **Scope:** convert `BookCatalog.Web.csproj` to SDK-style (`Microsoft.NET.Sdk.Web`, `net10.0`, per `global.json` 10.0.100); remove `packages.config` and the classic `Properties/AssemblyInfo.cs` if it duplicates generated attributes; add a minimal `Program.cs`; update `BookCatalog.sln` project entry; temporarily exclude not-yet-migrated classic files from compilation (see Sequencing notes); keep `NuGet.Config` source `nuget.org` (the classic `repositoryPath` setting is dropped here or in task 5 only if it breaks restore).
- **Allowed paths:** `src/BookCatalog.Web/**`, `BookCatalog.sln`, `NuGet.Config`, `global.json` (no change expected).
- **Validation:** from the source root: `dotnet build BookCatalog.sln -c Release` exits 0; then the scope gate.
- **Rollback / containment:** see Common Rules; no data or database is touched in this task.
- **Stop if:** the .NET 10 SDK is missing or `global.json` cannot be satisfied; restore needs a feed other than the configured `nuget.org`; the build requires files outside allowed paths.

### 002 - ASP.NET Core MVC hosting, routing, controllers, views, static content, validation, antiforgery, 404

- **Scope:** `Program.cs` with MVC and the default route; port `BooksController` (thin, DI-injected context; HTTP 404 via `NotFound()`; `Request.Headers.UserAgent` for the diagnostic box); port views to Core Razor (tag helpers or equivalent helpers) and `_ViewImports`; serve `Content/Site.css` at the same URL; apply the Behavior Preservation Rules; update footer text (FR-13); Development-only detailed errors (FR-14). Add EF Core package reference and a compile-only `ApplicationDbContext` per Sequencing notes. Delete `Global.asax*`, `App_Start/**`, `Web.config`, `Views/Web.config` once superseded.
- **Allowed paths:** `src/BookCatalog.Web/**`, `BookCatalog.sln`.
- **Validation:** `dotnet build BookCatalog.sln -c Release` exits 0; the scope gate; a database-free smoke check run locally against the built app: `GET /Content/Site.css` returns 200, `GET /Books/Create` returns 200 with an antiforgery field, a POST to Create without a token is rejected, an unknown route returns 404. (Runtime list/Details behavior needs the database and is proven in tasks 3-4.)
- **Rollback / containment:** see Common Rules; no database is created in this task.
- **Stop if:** a view or route cannot be preserved without changing visible content or URL shape; an adapter, `System.Web` shim, or YARP would be needed (D9); a package beyond FR-10 to FR-12 is needed.

### 003 - EF Core, dependency injection, deterministic seed-if-empty data, and migrations

- **Scope:** finalize EF Core (`Microsoft.EntityFrameworkCore.SqlServer`; design-time package recorded) with `AddDbContext`; connection string in `appsettings.json` per the Security review; initial migration for `Book` that preserves model constraints (Title 200, Author 100, ISBN 13); apply migrations at startup (`Database.Migrate()`), never `EnsureDeleted`/`EnsureCreated` per start; seed-if-empty startup logic with the seven fixed synthetic books; remove EF6 types, `BookCatalogInitializer`, the `DropCreateDatabaseIfModelChanges` behavior, `Directory.CreateDirectory`/`App_Data`, and every task-1 exclusion. Update `README.md` where classic setup steps are now wrong.
- **Allowed paths:** `src/BookCatalog.Web/**`, `BookCatalog.sln`, `README.md`.
- **Validation:** `dotnet build BookCatalog.sln -c Release` exits 0; scope gate; local run against LocalDB: first start creates `BookCatalogModernizedLab` with seven books (six listed at `/` and `/Books`); a second start does not reseed (AC-1, AC-8). Drop the database when finished.
- **Rollback / containment:** see Common Rules; additionally drop `BookCatalogModernizedLab` and delete task-created migration files if rolling back.
- **Stop if:** LocalDB is unavailable (record the missing prerequisite); a non-local or business database, secret, or password would be required; the schema must differ from the existing model constraints; an MDF or `App_Data` workaround seems necessary.

### 004 - tests/ project and local CRUD verification

- **Scope:** new test project under `tests/` added to `BookCatalog.sln`, using only packages needed for tests (recorded in the PR). Cover, where practical, AC-1 to AC-8: active-only title-ordered list; 404 on unknown ids (Details, Edit, Delete); valid create persisting `CreatedDate` and redirecting; invalid create/edit (missing Title/Author, Year 1799/1800/2100/2101, length boundaries 200/201, 100/101, 13/14) redisplaying with errors and saving nothing; edit persisting across a host restart without changing `CreatedDate` (AC-5 may instead be a recorded manual check on disposable data); hard delete and missing-id redirect; antiforgery rejection on Create/Edit/Delete POST; seed-if-empty. AC-9 (platform inspection / search for `System.Web`, EF6, `packages.config`, `Global.asax`, `debug="true"`, `customErrors`, old footer text) is a diff review and search, not a substitute for tests. Tests use a uniquely named disposable LocalDB database dropped on teardown.
- **Allowed paths:** `tests/**`, `BookCatalog.sln`, and minimal `src/BookCatalog.Web/**` changes needed for testability only (for example exposing `Program`), each recorded.
- **Validation:** from the source root: `dotnet build BookCatalog.sln -c Release` then `dotnet test` both exit 0 (AC-10); scope gate; recorded local CRUD check (create, edit, restart, delete) on `BookCatalogModernizedLab`, dropped afterwards.
- **Rollback / containment:** see Common Rules; drop temporary test databases.
- **Stop if:** a test failure shows a behavior change that cannot be fixed without changing an acceptance criterion; tests would need production data, secrets, or a shared database; LocalDB is unavailable (record it; do not report pass).

### 005 - Dependency CVE scan and bounded remediation

- **Scope:** scan the NuGet dependency graph (direct and transitive) of the projects in `BookCatalog.sln`; upgrade only vulnerable packages to the minimum patched version. Do not add packages unrelated to a CVE fix and do not add Azure SDKs.
- **Allowed paths:** `src/BookCatalog.Web/*.csproj`, `tests/**/*.csproj`, `NuGet.Config`, `BookCatalog.sln`; record every change and finding in the PR.
- **Validation:** `dotnet list BookCatalog.sln package --vulnerable --include-transitive` reports no known vulnerabilities (or each residual finding is documented); then `dotnet build BookCatalog.sln -c Release` and `dotnet test` both exit 0; scope gate.
- **Rollback / containment:** see Common Rules; project-file reverts only.
- **Stop if:** the vulnerability feed is unreachable (record it; do not report clean); a fix needs a major-version bump or a package change beyond FR-10 to FR-12, a route, or schema change, in which case document package, current and proposed versions and breaking-change risk, then ask; a remediation would change visible behavior.

---

## Migration Impact Summary

| Application  | Original                        | New                                     | Authentication | Comments |
|--------------|---------------------------------|-----------------------------------------|----------------|----------|
| BookCatalog  | ASP.NET MVC 5 / .NET FW 4.8     | ASP.NET Core MVC / .NET 10 (local only) | None (unchanged; no login) | No Azure or cloud service introduced |
| BookCatalog  | EF6 `DbContext` + LocalDB MDF   | EF Core (SQL Server provider) + LocalDB | Integrated Security (local), no password | New DB `BookCatalogModernizedLab`; no record migration |

---

## Open Questions & Questionnaire

- [x] Q: Infrastructure/environment provisioning? → A: No (Exclusions; D8; NFR-5).
- [x] Q: Integration testing against migrated Azure services? → A: Not applicable; local automated tests per decision Q2 are task 004.
- [x] Q: CVE remediation task? → A: Included as the final task at the request of the plan author, bounded by the spec's Dependencies constraint (task 005).
- [x] Q: Azure deployment target? → A: None. "No deployment is authorized"; `examples/azure/` needs separate approval.
- [x] Q: "Optional Azure-ready configuration without deploying"? → A: Out of scope (Exclusions; D8). Not planned; revisit only via a new approved specification.
- [ ] **Open item (human decision):** these plan artifacts live under `shared-legacy-app/.github/modernize/**`, which is outside the specification's allowed paths and would appear in `git diff --name-only` (AC-10). Decide whether to keep them out of the final change or approve their location.
- [ ] **Open item (human decision):** the tracked `packages/` folder contains EF6 files that must not be hand-edited, so AC-9 ("no EF6 ... remain") cannot be fully satisfied unless a human approves removing it. Task 003 must not touch it.
