# Assessment review notes: BookCatalog

Companion note to `reports-20261123190951/`. The generated report is unedited.
Findings below come from a Copilot review of the report against
`legacy-src/dotnet-bookcatalog/shared-legacy-app`, spot-checked by a human.

## Unsupported claim

- **Azure Blob Storage for "local file system".** Based on one call at
  `src/BookCatalog.Web/Global.asax.cs:15`, `Directory.CreateDirectory` for the
  LocalDB `DataDirectory`. No user files are stored. The folder disappears with
  the move to EF Core and Azure SQL.

## Not mentioned in the report

- ASP.NET MVC 5 / `System.Web`, `Global.asax` startup, `RouteConfig`,
  `FilterConfig`.
- `packages.config` package replacement.
- EF6 `DbContext`, `BookCatalogInitializer` and seed-if-empty logic
  (`Global.asax.cs`, `Models/ApplicationDbContext.cs`).
- `Web.config:16` `debug="true"` and `Web.config:18` `customErrors mode="Off"`.
- Behavior to preserve: active-only title ordering; antiforgery on POST
  actions; hard delete (`BooksController.cs:104`); `CreatedDate` set on create
  (`BooksController.cs:44`); `PublishedYear` range 1800-2100; seven seed books,
  one inactive.
- No test project and no recorded build result. Behavior is covered only by
  the manual checklist.

## Known limitation

The report is rule-based. Full analysis coverage (`analysisCoverage: full`)
was not run, so Codebase Insights are absent. The gaps above are carried into
the modernization specification instead.
