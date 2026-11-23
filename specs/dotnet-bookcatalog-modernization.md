# Change specification: Modernize BookCatalog to .NET 10 ASP.NET Core MVC and EF Core

## Metadata

- **Owner:** Workshop application team (human owner to confirm at approval)
- **Status:** Approved
- **Source root:** `legacy-src/dotnet-bookcatalog/shared-legacy-app`
- **Related issue:** not created

Evidence abbreviations used below (all repository-relative):

- **Notes:** `.github/modernize/assessment/bookcatalog/review-notes.md`
  (human-spot-checked findings and gaps in the Modernize report)
- **Report:** `.github/modernize/assessment/bookcatalog/reports-20261123190951/`
  (`index.md`, `repos/shared-legacy-app/report.md`; unedited, rule-based)
- **Src:** `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/`
- **Root:** `legacy-src/dotnet-bookcatalog/shared-legacy-app/`

## Problem and outcomes

### Problem

BookCatalog lets users add, view, edit, and delete book records and lists the
active books sorted by title (`Root/README.md`). It is an ASP.NET MVC 5,
.NET Framework 4.8, EF6, SQL Server LocalDB application (`Root/README.md`;
`Src/Web.config`; `Src/packages.config`). The Report classifies it as
.NET Framework 4.8, recommends "Migrate to .NET 10", 6R **Replatform**, and
lists six issues: one mandatory, two potential, and three optional
(`Report/index.md`; `Report/repos/shared-legacy-app/report.md`).

Platform dependencies that block a supported runtime include `System.Web`,
`Global.asax` startup, `RouteConfig`, `FilterConfig`, `packages.config`, EF6
`DbContext` with a `DropCreateDatabaseIfModelChanges` initializer, and
LocalDB attached by `AttachDbFilename` (`Src/Global.asax.cs`;
`Src/App_Start/`; `Src/Models/ApplicationDbContext.cs`; `Src/Web.config`).
The Report omits most of these; Notes records them as gaps to carry into this
specification.

### Desired outcomes

1. Users see the same book catalog behavior after the move (FR-1 to FR-9).
2. The application runs as ASP.NET Core MVC on .NET 10 using EF Core, with no
   `System.Web`, EF6, or `packages.config` dependency (FR-10 to FR-14).
3. Behavior that was previously unguarded becomes verifiable (AC-1 to AC-10),
   because no test project or recorded build result exists today (Notes).

### Success measures

- Every acceptance criterion passes with recorded evidence.
- The primary validation command (see Constraints) exits 0 and its result is
  recorded.
- `git diff --name-only` shows changes only within the allowed paths.

## Functional requirements

### Behavior to preserve

| ID | Requirement | Evidence |
| --- | --- | --- |
| FR-1 | The default route opens the books list (`Books/Index`); the route pattern is `{controller}/{action}/{id}` with optional `id`. | `Src/App_Start/RouteConfig.cs` |
| FR-2 | The list shall show only active books, ordered by title. | `Src/Controllers/BooksController.cs` `Index`; Notes |
| FR-3 | Details, Edit, and Delete for an unknown id shall return HTTP 404. | `BooksController.cs` `Details`, `Edit`, `Delete`, `Edit` POST |
| FR-4 | Create shall accept Title (required, max 200), Author (required, max 100), ISBN (optional, max 13), Published Year (optional, 1800–2100), and Active, and shall set `CreatedDate` at creation; invalid input shall redisplay the form without saving. | `Src/Models/Book.cs`; `BooksController.cs:44`; Notes |
| FR-5 | A new Book defaults to Active = true and `CreatedDate` = now in local time (`DateTime.Now`) on the model; UTC would be a separate deliberate change (decision Q5). | `Src/Models/Book.cs` constructor |
| FR-6 | Edit shall update Title, Author, ISBN, Published Year, and Active only; it shall not change `CreatedDate`. | `BooksController.cs` `Edit` POST |
| FR-7 | Delete shall be a confirmation page followed by a POST that permanently removes the record (hard delete); deleting an already-missing id shall redirect to the list without error. | `BooksController.cs:99` `DeleteConfirmed`; Notes |
| FR-8 | All state-changing POST actions (Create, Edit, Delete) shall require an antiforgery token. | `BooksController.cs` `[ValidateAntiForgeryToken]`; Notes |
| FR-9 | An empty database shall be seeded once at startup with seven books, six active and one inactive ("The Matrix: The Shooting Script"), and a populated database shall not be reseeded. | `Src/Global.asax.cs`; `Src/Models/ApplicationDbContext.cs` `SeedBooks`; Notes; `Root/README.md` |

The visible page content, labels, columns, and the existing list/detail/form
layout are preserved as-is, including the legacy decorative and diagnostic
elements: the `YOUR BROWSER` user-agent box, the static visitor counter
`001337`, the static `Page generated in 0.042 seconds` text, and the
Internet Explorer hint (`Src/Controllers/BooksController.cs` `Index`;
`Src/Views/Books/Index.cshtml`; `Src/Views/Shared/_Layout.cshtml`).
Footer text naming ASP.NET MVC 5, EF 6, or .NET Framework 4.8 is the one
exception under FR-13.
Removing these elements is a separate change (decision Q3).

### Intentionally changed behavior

| ID | Requirement | Evidence or rationale |
| --- | --- | --- |
| FR-10 | The web host shall be ASP.NET Core MVC targeting .NET 10, replacing `System.Web`, `Global.asax`, `RouteConfig`, and `FilterConfig` startup. | Direction in request; `Root/global.json` (SDK 10.0.100); `Root/README.md`; Notes |
| FR-11 | Data access shall use EF Core with the SQL Server provider, replacing the EF6 `DbContext` and `DropCreateDatabaseIfModelChanges` initializer. Local development shall use LocalDB with a new database named `BookCatalogModernizedLab`. | `Src/Models/ApplicationDbContext.cs`; `Root/README.md`; decision Q4 |
| FR-12 | Seed-if-empty shall be preserved as startup logic that does not reset existing data on every start, so an edit survives an application restart. | `Src/Global.asax.cs`; `Root/README.md` |
| FR-13 | Footer text that names ASP.NET MVC 5, Entity Framework 6, or .NET Framework 4.8 shall be updated to the new platform. | `Src/Views/Shared/_Layout.cshtml` (static claim becomes false) |
| FR-14 | `Web.config` `debug="true"` and `customErrors mode="Off"` shall not be carried forward as production-style defaults; detailed errors shall be limited to development. | `Src/Web.config:16,18`; Notes |
| FR-15 | The `Directory.CreateDirectory` call for LocalDB `DataDirectory`, and the folder it creates, shall be removed. No user files are stored. | `Src/Global.asax.cs:16` (Notes and Report cite line 15; the call is on line 16); Notes (the Report's "Azure Blob Storage for local file system" claim is unsupported) |
| FR-16 | Existing records need not survive; the database is disposable sample data. | `Root/README.md` |

## Constraints and out of scope

### Constraints

- **Allowed paths** (all inside the source root):
  - `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/**`
  - `legacy-src/dotnet-bookcatalog/shared-legacy-app/BookCatalog.sln`
  - `legacy-src/dotnet-bookcatalog/shared-legacy-app/global.json`
  - `legacy-src/dotnet-bookcatalog/shared-legacy-app/NuGet.Config`
  - `legacy-src/dotnet-bookcatalog/shared-legacy-app/README.md`
  - `legacy-src/dotnet-bookcatalog/shared-legacy-app/tests/**` (new test
    project, allowed by decision Q2)
  - This specification file for status updates only.
- **Never edit:** `packages/`, `bin/`, `obj/`, generated database files, or an
  empty MDF created as a workaround (`legacy-src/dotnet-bookcatalog/AGENTS.md`).
- **Primary validation command** (run from the source root, non-interactive):
  - *Baseline, before any change:*
    `msbuild BookCatalog.sln -restore -p:Configuration=Release`
    (`legacy-src/dotnet-bookcatalog/AGENTS.md`; Lab 01). The classic baseline
    needs Windows, Visual Studio MSBuild/web tooling, .NET Framework 4.8
    targeting tools, and `nuget restore`. Observed baseline: see the review
    checkpoint.
  - *After modernization (decision Q1):*
    `dotnet build BookCatalog.sln -c Release`, then `dotnet test` once the
    project is SDK-style. Do not substitute another command without human
    approval.
- **Compatibility:** Preserve FR-1 to FR-9 and the visible UI. Routes keep
  their current URL shape.
- **Dependencies:** Use only packages needed for FR-10 to FR-12. Any package
  addition must be recorded in the pull request. Do not reuse
  `System.Web` adapters, YARP, a side-by-side host, or a shared schema
  (`Root/README.md`).
- **Data and security:** see "Security, data, cost, and deployment" below.

### Exclusions (out of scope)

- Any path outside the source root, including `legacy-src/dotnet-bookcatalog/examples/azure/`,
  other `legacy-src/` applications, and `capstone-src/`.
- Azure deployment, provisioning, or any cloud resource. The Report's
  recommendations for Azure Blob Storage, Key Vault, Entra ID, App Service MI,
  and Azure SQL are not requirements (`Report/index.md`); the Blob Storage
  claim is unsupported (Notes).
- New authentication or authorization. The Report's "Windows authentication
  detected" finding cites `Web.config` only (`Report/repos/shared-legacy-app/report.md`); no
  user-facing login exists in the source.
- Data transfer or migration of existing LocalDB records (`Root/README.md`).
- New features, visual redesign, API endpoints, or behavior changes not listed
  under "Intentionally changed behavior".
- Removing or editing the imported attribution, provenance, or notices.

## Edge cases

| Type | Condition | Expected behavior |
| --- | --- | --- |
| Normal | Active and inactive books exist | Only active books are listed, in title order. |
| Failure | Details, Edit, or Delete requests an id that does not exist | HTTP 404 (Delete POST redirects to the list). |
| Failure | Create or Edit submitted with a missing Title or Author | Form redisplays with validation errors; nothing is saved. |
| Failure | POST without a valid antiforgery token | Request is rejected; no change is saved. |
| Boundary | Published Year 1799, 1800, 2100, 2101 | 1799 and 2101 rejected; 1800 and 2100 accepted; blank accepted. |
| Boundary | Title 200/201 chars; Author 100/101; ISBN 13/14 | Lengths up to the maximum accepted; longer rejected. |
| Boundary | No active books | List shows the existing "No books found." message. |
| Compatibility | Restart with existing data | Existing records remain; seed does not re-run. |
| Compatibility | Fresh empty database | Seven books are seeded; six are listed. |

## Acceptance criteria

### AC-1: Active-only title-ordered list

- **Given** the seeded database
- **When** a user opens `/` and `/Books`
- **Then** six active books are listed in title order and the inactive book is
  not listed
- **Verification:** automated test or recorded manual check

### AC-2: Details and unknown ids

- **Given** an existing book and a nonexistent id
- **When** a user opens Details, Edit, and Delete for each
- **Then** the existing book displays and the nonexistent id returns HTTP 404
- **Verification:** automated test or recorded manual check

### AC-3: Valid create

- **Given** valid Title and Author
- **When** the form is submitted with a valid antiforgery token
- **Then** the book is saved with `CreatedDate` set, and the user is
  redirected to the list
- **Verification:** automated test or recorded manual check

### AC-4: Invalid create and edit

- **Given** a missing Title/Author or a Published Year outside 1800–2100
- **When** the form is submitted
- **Then** validation errors display and no record changes
- **Verification:** automated test or recorded manual check

### AC-5: Edit persists and keeps CreatedDate

- **Given** an existing book
- **When** the user edits it and the application is restarted
- **Then** the edited values remain and `CreatedDate` is unchanged
- **Verification:** recorded manual check on disposable data

### AC-6: Hard delete

- **Given** an existing book
- **When** the user confirms Delete
- **Then** the record is permanently removed and no longer displayed
- **Verification:** automated test or recorded manual check

### AC-7: Antiforgery on POST

- **Given** a Create, Edit, or Delete POST without a valid token
- **When** it is submitted
- **Then** it is rejected and nothing changes
- **Verification:** automated test or recorded manual check

### AC-8: Seed-if-empty

- **Given** an empty database, then a populated one
- **When** the application starts each time
- **Then** seven books are seeded once; the second start does not reseed
- **Verification:** recorded manual check or automated test

### AC-9: Platform change and configuration

- **Given** the modernized source
- **When** it is inspected
- **Then** it targets .NET 10 ASP.NET Core MVC and EF Core; no `System.Web`,
  EF6, `packages.config`, `Global.asax`, `debug="true"`, or
  `customErrors mode="Off"` equivalents remain; and footer text no longer
  names MVC 5, EF 6, or .NET Framework 4.8
- **Scope note (2026-11-23, human decision):** "remain" applies to the project,
  source, and configuration files under `src/`, `tests/`, and the solution.
  The tracked legacy `packages/` folder is excluded from this check and is not
  edited or deleted.
- **Verification:** diff review and search of the source root, excluding
  `packages/`

### AC-10: Build and scope

- **Given** the documented prerequisites
- **When** `dotnet build BookCatalog.sln -c Release` and then `dotnet test`
  run from the source root
- **Then** both exit 0, and `git diff --name-only` lists only allowed paths
- **Verification:** record command, exit status, and result

## Non-functional requirements

| ID | Quality | Testable requirement | Verification |
| --- | --- | --- | --- |
| NFR-1 | Security/privacy | No secret, credential, password, or real data is added to source, configuration, or fixtures. | Diff review |
| NFR-2 | Security/privacy | Connection settings are disposable and local only; no business or production database. | Config review |
| NFR-3 | Reliability | Startup does not reset or recreate data on every start. | AC-5, AC-8 |
| NFR-4 | Maintainability | Controllers stay thin and model validation remains declared on the model. | Diff review (`.github/instructions/dotnet.instructions.md`) |
| NFR-5 | Cost | No Azure resource is created or required to complete this change. | Diff and command review |

## Security, data, cost, and deployment

- **Security:** The sample uses integrated authentication to LocalDB and no SQL
  password (`Src/Web.config`; `Root/README.md`). Do not add secrets and do not
  point it at a business database. The `debug="true"` and
  `customErrors mode="Off"` settings are not carried forward (FR-14).
- **Data:** All data is disposable sample data. Use only the seven synthetic
  seed books or visibly synthetic test records. Do not import records.
- **Cost:** The Report's ~$63/month figure is directional, for an assumed
  Azure dev/test baseline, and is not approved spend (`Report/index.md`). This
  change creates no billable resource.
- **Deployment:** No deployment is authorized. Azure assets under
  `legacy-src/dotnet-bookcatalog/examples/azure/` require separate access,
  scope, and cost approval and are outside this specification.

## Cleanup obligations

- Remove the LocalDB `DataDirectory` creation and the `App_Data` folder it
  produced (FR-15). Do not precreate `App_Data` or an empty MDF.
- Remove classic artifacts made unnecessary by the migration only within
  allowed paths (for example `Global.asax`, `App_Start`, `packages.config`,
  `Web.config`), and do not hand-edit `packages/`, `bin/`, or `obj/`.
- Leave no generated database files, local databases, or temporary files in
  the repository.
- Update `Root/README.md` where its classic setup steps become inaccurate.
- Drop any temporary database created during validation.

## Assumptions, dependencies, and risks

| Item | Type | Validation or mitigation |
| --- | --- | --- |
| The Report is rule-based; `analysisCoverage: full` was not run, so Codebase Insights are absent. | Risk | Treat Notes and source as authoritative; do not infer facts from the Report alone (Notes). |
| The Report's decision for the component is "Blocked" with one mandatory issue. | Risk | Confirm during approval that the blocker is understood (`Report/index.md`). |
| No test project exists in the baseline. | Risk | A new test project under `tests/` is allowed (Q2) and must cover AC-1 to AC-8 where practical (Notes). |
| Baseline needs Windows, VS MSBuild, .NET Framework 4.8 targeting tools, LocalDB. | Dependency | Record missing prerequisites instead of reporting a pass (`legacy-src/dotnet-bookcatalog/AGENTS.md`). |
| .NET SDK 10 or later is required. | Dependency | `Root/global.json` pins 10.0.100 with `rollForward: latestMajor`. |
| Report findings about SQL connection, `System.Data.SqlClient`, and connection strings point to `Web.config`. | Assumption | Verified against `Src/Web.config`; these are resolved by FR-11 and FR-14. |

## Technical decisions

Recorded during reconciliation of the Report and Notes against source on
2026-11-23. Each decision implements an existing requirement; none changes
intent, constraints, acceptance criteria, allowed paths, validation, or stop
conditions. No ADR is used.

| # | Decision | Basis |
| --- | --- | --- |
| D1 | Target .NET 10 and ASP.NET Core MVC in a single host. | FR-10; `Root/global.json`; Report recommends "Migrate to .NET 10" |
| D2 | Replace EF6 with EF Core and the SQL Server provider. | FR-11; `Src/Models/ApplicationDbContext.cs` |
| D3 | Preserve active-only title ordering, CRUD, model validation, antiforgery on POST, and persistence across restart. | FR-2 to FR-9, FR-12; AC-1 to AC-8 |
| D4 | Data is disposable workshop data. Create and seed a new `BookCatalogModernizedLab` LocalDB database; do not migrate records from the old MDF. | FR-11, FR-16; Q4; `Root/README.md` |
| D5 | The request user-agent diagnostic (`YOUR BROWSER`) is preserved as-is, with the other legacy UI elements. | Q3; `Src/Controllers/BooksController.cs` `Index`; `Src/Views/Books/Index.cshtml:67-70` |
| D6 | Use EF Core migrations to create the schema, applied at startup. Never delete and recreate the database on every startup; seed only when empty. The EF Core design-time package is recorded in the pull request. | FR-12; NFR-3; AC-5, AC-8; Dependencies constraint |
| D7 | Local integrated authentication to LocalDB, with no password in configuration. The Report's "Windows authentication detected" finding reflects `Integrated Security=True` in `Src/Web.config`; no user-facing login is added. | NFR-1, NFR-2; Security section; Exclusions |
| D8 | Azure is considered only after local verification and is out of scope for now. | NFR-5; Exclusions; Deployment section |
| D9 | No side-by-side `System.Web` host, YARP proxy, shared legacy schema, or `System.Web` adapters. | Dependencies constraint; `Root/README.md` |

### Evidence reconciliation (2026-11-23)

- Report: 6 issues (1 mandatory, 2 potential, 3 optional); Blocked; 6R
  Replatform; ~$63/month estimate; no Codebase Insights. Confirmed against
  `Report/index.md` and `Report/repos/shared-legacy-app/report.md`.
- Source citations confirmed: route pattern, `Index` filter and ordering,
  `Create` `CreatedDate` (`BooksController.cs:44`), `DeleteConfirmed` (line 99,
  not 104 as in Notes), `Web.config` lines 16 and 18,
  `Directory.CreateDirectory` (`Global.asax.cs:16`, not 15), model validation
  ranges, seven seed books with one inactive, and footer and legacy UI text in
  `Src/Views/Shared/_Layout.cshtml`.
- No `tests/` directory exists yet, matching Notes.

## Plan-mode review checkpoint

### Evidence

- **Current behavior:** `Src/Controllers/BooksController.cs`,
  `Src/Models/Book.cs`, `Src/Models/ApplicationDbContext.cs`,
  `Src/Global.asax.cs`, `Src/Web.config`, `Src/App_Start/RouteConfig.cs`,
  `Src/Views/**` (read during drafting).
- **Existing tests:** none found under the source root (Notes;
  `legacy-src/dotnet-bookcatalog/AGENTS.md`).
- **Baseline validation evidence:**
  `.\scripts\validation\validate-dotnet.ps1 -Configuration Release` — exit 0
  on 2026-11-23, producing `BookCatalog.Web.dll` (result reported by the
  human owner; not re-run while drafting).

### Unresolved questions

Resolved by the human owner on 2026-11-23 and reflected above:

- [x] **Q1:** Post-change validation is `dotnet build BookCatalog.sln -c Release`
      then `dotnet test` (once the project is SDK-style).
- [x] **Q2:** A new test project is allowed inside `shared-legacy-app`
      (under `tests/`).
- [x] **Q3:** Preserve the legacy UI elements as-is (user-agent box, visitor
      counter, static timing text, IE hint); any removal is a separate change.
- [x] **Q4:** EF Core SQL Server provider; LocalDB for local development; new
      database `BookCatalogModernizedLab`.
- [x] **Q5:** Keep local time (`DateTime.Now`) for `CreatedDate`; UTC would be
      a separate deliberate change.

No unresolved questions remain. The specification stays Draft until human
approval below.

### Boundary and validation review

- [ ] Evidence citations were checked against the repository.
- [ ] Allowed paths include all anticipated implementation and test changes and
      nothing unrelated.
- [ ] The primary validation command is safe, non-interactive, and runnable
      from the stated source root.
- [ ] Acceptance criteria cover the required normal, failure, boundary, and
      compatibility behavior.
- [ ] Exclusions prevent unrelated modernization or cleanup.

### Stop conditions

Stop and request human guidance if:

- repository evidence contradicts this specification;
- a new question arises that could change behavior, scope, or validation;
- a criterion requires a path outside the allowed paths;
- the baseline or primary validation fails for a reason caused by the change,
  or its command is missing, destructive, or irreproducible;
- a required prerequisite (Windows tooling, .NET 10 SDK, LocalDB) is missing;
- a secret, credential, production or business database, Azure resource,
  deployment, or cost is required;
- a schema, public route, or dependency change beyond FR-10 to FR-12 is
  required;
- the change would affect another application, track, or `examples/azure/`; or
- the acceptance criteria cannot be met without expanding this contract.

### Human approval

- [x] I reviewed the evidence, unresolved questions, allowed paths, acceptance
      criteria, validation command, exclusions, risks, and stop conditions.
- **Reviewer:** jmacdoug-GOS (self-review; no second reviewer in this workshop run)
- **Approval date:** 2026-11-23
- **Approved specification commit:** 3a5679f

Change **Status** to `Approved` only after the checkbox is selected and the
reviewer, date, and commit fields are complete. Planning approval authorizes
issue preparation, not implementation.
