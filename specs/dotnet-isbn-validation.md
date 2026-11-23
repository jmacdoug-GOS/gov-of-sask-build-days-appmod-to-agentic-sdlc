# Change specification: BookCatalog ISBN-13 validation

## Metadata

- **Owner:** Unknown (not provided; to be assigned by the maintainer)
- **Status:** Draft
- **Source root:** `legacy-src/dotnet-bookcatalog/shared-legacy-app`
- **Related issue:** not created

## Problem and outcomes

### Problem

Users of the legacy BookCatalog web app can save a book whose ISBN is not a
valid ISBN-13. `Book.ISBN` carries only a length cap, so values such as `abc`,
`12-34`, or a 13-digit number with a wrong check digit pass model validation
and are persisted.

Evidence:

- `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/Models/Book.cs:Book.ISBN` has only `[StringLength(13)]`; it is not `[Required]` and has no format or checksum rule.
- `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/Controllers/BooksController.cs:Create(Book)` and `Edit(Book)` are `[HttpPost][ValidateAntiForgeryToken]` and call `db.SaveChanges()` whenever `ModelState.IsValid`.
- `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/Views/Books/Create.cshtml` and `Edit.cshtml` render `TextBoxFor(m => m.ISBN, new { maxlength = "13" })` and `ValidationMessageFor(m => m.ISBN)`.
- `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/Models/ApplicationDbContext.cs:BookCatalogInitializer.SeedBooks` seeds 13-digit ISBNs, which suggests the intended format (inference, not documented intent).

### Desired outcomes

1. A non-blank ISBN is accepted on Create and Edit only when it is exactly 13 ASCII digits with a valid ISBN-13 check digit; otherwise the user sees a clear field-level error and nothing is saved.
2. A blank ISBN remains valid, and no stored data, schema, or other behavior changes.

### Success measures

- Every row of the acceptance criteria below behaves as stated on both Create and Edit, as recorded in the manual checklist results.
- The primary validation command exits 0.

## Functional requirements

| ID | Requirement | Evidence or rationale |
| --- | --- | --- |
| FR-1 | A null or empty ISBN shall remain valid. | ISBN is optional today: `Models/Book.cs:Book.ISBN` has no `[Required]`. User decision: ISBN stays optional. |
| FR-2 | A non-null, non-empty ISBN shall be valid only if it is exactly 13 characters, each ASCII `0`-`9`. Whitespace-only values, leading or trailing whitespace, hyphens, spaces, letters, and non-ASCII digits shall be rejected. | User decision: digits only; hyphens/spaces rejected; whitespace-only and leading/trailing whitespace are format errors. |
| FR-3 | A 13-digit ISBN shall additionally be valid only if its ISBN-13 check digit is correct. | User decision: ISBN-13 with checksum validation. |
| FR-4 | The format failure message shall be exactly `ISBN must be exactly 13 digits.` | User decision. |
| FR-5 | The checksum failure message shall be exactly `ISBN check digit is invalid.` | User decision: separate message. |
| FR-6 | Validation shall be a server-side `ValidationAttribute` applied to `Book.ISBN`; errors shall be field-level and shown by the existing `ValidationMessageFor(m => m.ISBN)`. | User decision. |
| FR-7 | `[StringLength(13)]` shall remain on `Book.ISBN` with `ErrorMessage = "ISBN must be exactly 13 digits."`, and the custom attribute shall not report length errors that `StringLength` already reports, so a value longer than 13 characters shows exactly one message. | User decision. |
| FR-8 | On any ISBN validation failure, Create and Edit shall redisplay the form with the entered values and shall not save (no `SaveChanges()`), and Create and Edit shall behave identically. | `BooksController.cs:Create(Book)`/`Edit(Book)` return `View(book)` when `ModelState` is invalid. |
| FR-9 | Existing stored records shall not be modified. Editing a record whose stored ISBN is invalid shall require the user to correct the ISBN. | User decision; `Edit(Book)` revalidates the whole model. |
| FR-10 | The attribute shall not throw for any input (null, empty, very long, non-ASCII) and shall not echo the submitted value in its message. | `Web.config` has `customErrors mode="Off"`, so unhandled exceptions are exposed. |

## Constraints and out of scope

### Constraints

- **Allowed paths:**
  - `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/Models/Book.cs`
  - `specs/dotnet-isbn-validation.md`
- **Primary validation command:** `msbuild BookCatalog.sln -restore -p:Configuration=Release` run from `legacy-src/dotnet-bookcatalog/shared-legacy-app` (the repository wrapper `pwsh scripts/validation/validate-dotnet.ps1 -Configuration Release` is accepted by the participant as an equivalent substitute although it lies outside the source root). It proves compilation only.
- **Compatibility:** Target framework, packages, views, controller, EF model and database schema are unchanged. The attribute must not change the EF6 model, because `BookCatalogInitializer` is `DropCreateDatabaseIfModelChanges`. Unknown: whether adding the attribute changes the model hash; this must be confirmed by running against a disposable LocalDB.
- **Dependencies:** No new packages or dependency changes. The attribute class is defined inside `Models/Book.cs` because `BookCatalog.Web.csproj` lists `<Compile Include>` entries explicitly (lines 110-118) and the csproj is not an allowed path.
- **Data and security:** ISBN is non-personal catalog data. Use only synthetic ISBNs in testing. No secrets, no real data. Antiforgery behavior is unchanged. The sample LocalDB database is disposable and must never be pointed at a business database (see `shared-legacy-app/README.md`).

### Exclusions (out of scope)

- Normalizing or stripping hyphens/spaces (explicit non-goal).
- Client-side validation (possible follow-up spec).
- Data migration, cleanup, or reporting of existing invalid ISBNs.
- Database schema changes and seed data changes.
- ISBN-10 support.
- A new automated test project (follow-up).
- Changes to views, controller, csproj, packages, `Web.config`, other modernization gaps (User-Agent display, hard delete, `customErrors`), the Java track, or any other source root.

## Edge cases

| Type | Condition | Expected behavior |
| --- | --- | --- |
| Normal | `9780132350884` (valid ISBN-13, synthetic example from seed data) | Saves; redirects to Index. |
| Failure | `9780132350885` (13 digits, wrong check digit) | Shows `ISBN check digit is invalid.`; not saved. |
| Failure | `978013235088` (12 digits), `97801323508AB`, `978-0-13-235088-4`, `9780132350884 ` (trailing space), `   ` (spaces only) | Shows `ISBN must be exactly 13 digits.`; not saved. |
| Boundary | Empty / blank field | Saves (MVC binds empty string to null). |
| Boundary | `97801323508840` (14 digits) | Exactly one error message: `ISBN must be exactly 13 digits.` |
| Boundary | Non-ASCII digits (e.g. Arabic-Indic) | Format error; no exception. |
| Compatibility | Existing record with an invalid stored ISBN is opened in Edit and saved unchanged | Rejected until ISBN is corrected or cleared; stored record unchanged. |
| Compatibility | Seed ISBNs in `SeedBooks` | Each must pass the check digit rule (unverified; see questions). |

## Acceptance criteria

Each criterion is verified on both Create and Edit.

### AC-1: Valid ISBN-13 saves

- **Given** the Create or Edit form with valid other fields
- **When** ISBN is `9780132350884` and the form is submitted
- **Then** the book is saved and the user is redirected to Index
- **Verification:** Manual checklist; confirm on Details.

### AC-2: Blank ISBN saves

- **Given** the Create or Edit form with valid other fields
- **When** ISBN is left empty and the form is submitted
- **Then** the book is saved
- **Verification:** Manual checklist.

### AC-3: Format errors

- **Given** the Create or Edit form with valid other fields
- **When** ISBN is each of `978013235088`, `97801323508AB`, `978-0-13-235088-4`, `9780132350884 ` (trailing space), `   ` (spaces only)
- **Then** the form is redisplayed with the entered value and `ISBN must be exactly 13 digits.` next to ISBN, and nothing is saved
- **Verification:** Manual checklist; for Edit, reopen Details and confirm the record is unchanged.

### AC-4: Checksum error

- **Given** the Create or Edit form with valid other fields
- **When** ISBN is `9780132350885`
- **Then** the form is redisplayed with `ISBN check digit is invalid.` and nothing is saved
- **Verification:** Manual checklist.

### AC-5: Too-long value shows one message

- **Given** the Create or Edit form with valid other fields
- **When** ISBN is `97801323508840` (14 digits), submitted by a crafted POST including the antiforgery token, because the views set `maxlength="13"`
- **Then** exactly one error message is shown for ISBN, `ISBN must be exactly 13 digits.`
- **Verification:** Manual crafted POST using synthetic data; inspect the response for a single ISBN message.

### AC-6: Existing invalid data is untouched

- **Given** a stored record whose ISBN is invalid (created in a disposable database)
- **When** the record is opened in Edit and saved without changing the ISBN
- **Then** the save is rejected with an ISBN error and the stored record is unchanged; correcting the ISBN allows the save
- **Verification:** Manual checklist against disposable LocalDB.

### AC-7: Build and database preserved

- **Given** the baseline build passes before the change
- **When** the primary validation command is run after the change and the app is started against an existing disposable LocalDB containing data
- **Then** the build exits 0, and the existing data persists (the database is not recreated)
- **Verification:** Primary validation command; observe Index after restart.

## Non-functional requirements

| ID | Quality | Testable requirement | Verification |
| --- | --- | --- | --- |
| NFR-1 | Reliability | The attribute never throws for null, empty, long, or non-ASCII input. | Manual checklist cases; code review. |
| NFR-2 | Security/privacy | Error messages are fixed strings and do not echo input; test data is synthetic. | Code review; checklist. |
| NFR-3 | Maintainability | Only `Models/Book.cs` changes in application code; no new dependencies. | `git diff --name-only`. |

## Assumptions, dependencies, and risks

| Item | Type | Validation or mitigation |
| --- | --- | --- |
| Windows, Visual Studio MSBuild, and `MSSQLLocalDB` are available. | Dependency (unknown) | Record exact unavailable prerequisite and stop if missing. |
| Behavior is verified manually and is not repeatable in CI; the build proves compilation only. | Risk (accepted by the maintainer) | Record observed results; test project is a follow-up. |
| Adding the attribute does not trigger `DropCreateDatabaseIfModelChanges`. | Assumption (unverified) | AC-7; stop if the database is recreated. |
| The default MVC binder converts empty ISBN to null but not whitespace. | Assumption (framework behavior, unverified here) | AC-2 and AC-3 manual cases. |
| All seven seed ISBNs pass the check digit rule. | Verified by computing each check digit | Stop if seed data changes and any fails. |
| A legacy record with an invalid ISBN cannot be edited until corrected. | Risk (accepted) | Documented behavior (FR-9, AC-6). |

## Plan-mode review checkpoint

### Evidence

- **Current behavior:** `legacy-src/dotnet-bookcatalog/shared-legacy-app/src/BookCatalog.Web/Models/Book.cs:Book.ISBN` — `[StringLength(13)]` only; flows through `Controllers/BooksController.cs:Create(Book)`/`Edit(Book)` to `Models/ApplicationDbContext.cs:ApplicationDbContext.Books` and `SaveChanges()`.
- **Existing tests:** none found under `legacy-src/dotnet-bookcatalog`. `scripts/validation/Test-BookCatalogSmoke.ps1` targets a net10.0 build, not this baseline; whether it asserts ISBN behavior is unknown (not read in full).
- **Baseline validation evidence:** `msbuild BookCatalog.sln -restore -p:Configuration=Release` — not yet run; result unknown.

### Unresolved questions

- [x] Is `pwsh scripts/validation/validate-dotnet.ps1 -Configuration Release` an acceptable substitute for the primary command? Resolved: accepted by the participant.
- [x] Do all seven seed ISBNs pass the check digit rule? Resolved: all seven were computed and pass.
- [ ] Who owns this specification and who is the approving reviewer? Owner: maintainer; deadline: not set.
- [ ] Is the allowed-path set (`Models/Book.cs` plus this spec) confirmed, given a new attribute class lives in `Book.cs`? Owner: maintainer; deadline: not set.

The specification is not ready for approval while a question can change scope,
acceptance criteria, allowed paths, exclusions, or validation.

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
- an unresolved question could materially change behavior or scope;
- satisfying a criterion requires a path outside the allowed paths (including the csproj, views, controller, packages, or target framework);
- the baseline or primary validation fails for a reason caused by the proposed
  change, or the command is missing, destructive, or irreproducible;
- a missing prerequisite (Windows, MSBuild, LocalDB) prevents required evidence from being collected;
- a secret, credential, production resource, destructive action, dependency
  change, schema change, or public API change is required but not explicitly
  approved;
- stored data or the database schema would need to change, or the database would be recreated;
- input normalization would be needed;
- a seed ISBN fails the new rule;
- a duplicate or wrong ISBN message cannot be fixed within `Models/Book.cs`; or
- implementation or validation cannot satisfy the acceptance criteria without
  expanding the approved contract.

### Human approval

- [ ] I reviewed the evidence, unresolved questions, allowed paths, acceptance
      criteria, validation command, exclusions, risks, and stop conditions.
- **Reviewer:** not yet assigned
- **Approval date:** not yet approved
- **Approved specification commit:** not yet approved

Change **Status** to `Approved` only after the checkbox is selected and the
reviewer, date, and commit fields are complete. Planning approval authorizes
issue preparation, not implementation.
