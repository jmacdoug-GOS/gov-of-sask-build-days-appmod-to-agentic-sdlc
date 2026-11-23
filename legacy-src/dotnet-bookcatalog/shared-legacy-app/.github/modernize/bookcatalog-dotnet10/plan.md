# Modernization Plan: BookCatalog .NET 10 Modernization

**Project**: BookCatalog (shared-legacy-app)

---

## Technical Framework

- **Language**: C# (.NET Framework 4.8)
- **Framework**: ASP.NET MVC 5, `System.Web`, `Global.asax` startup, `RouteConfig`/`FilterConfig`
- **Build Tool**: MSBuild (`nuget restore` + `packages.config`)
- **Database**: SQL Server LocalDB via EF6 `DbContext` with `DropCreateDatabaseIfModelChanges`
- **Key Dependencies**: Entity Framework 6, `packages.config`-managed NuGet packages

---

## Overview

This migration moves BookCatalog from ASP.NET MVC 5 on .NET Framework 4.8
with EF6 to ASP.NET Core MVC on .NET 10 with EF Core, per the approved
specification (`specs/dotnet-bookcatalog-modernization.md`). The application
currently serves CRUD book-catalog pages backed by a LocalDB database seeded
once at startup. The new architecture will:

- Run on a supported, SDK-style .NET 10 host, removing `System.Web`,
  `Global.asax`, and `packages.config` so the project can use modern tooling
  and future Azure SDKs.
- Replace EF6 with EF Core (SQL Server provider) using EF Core migrations
  applied at startup, preserving seed-if-empty semantics and a new local
  LocalDB database (`BookCatalogModernizedLab`) rather than migrating old
  records.
- Add an automated test project (none exists today) covering the preserved
  CRUD, validation, antiforgery, 404, and seed-if-empty behavior so this
  behavior becomes verifiable going forward.
- Remediate known CVEs in NuGet dependencies as part of the move to
  SDK-style package references.

The migration is local-only: it runs as a single upgrade effort (framework,
web host, and data access together, per the .NET upgrade guideline), followed
by dependency security remediation. No infrastructure, containerization, or
deployment step is included — Azure readiness beyond the already-supported
.NET 10/EF Core baseline is explicitly out of scope for this change (see
"Open Questions & Questionnaire").

---

## Migration Impact Summary

| Application  | Original Service              | New Azure Service | Authentication | Comments |
|--------------|--------------------------------|--------------------|-----------------|----------|
| BookCatalog  | ASP.NET MVC 5 / .NET FW 4.8     | ASP.NET Core MVC / .NET 10 (local only) | N/A — no cloud target | No Azure service is introduced; local LocalDB only |
| BookCatalog  | EF6 `DbContext` + LocalDB MDF   | EF Core (SQL Server provider) + LocalDB | Integrated Security (local) | New DB name `BookCatalogModernizedLab`; no record migration |

---

## Open Questions & Questionnaire

- [x] Q: Should the plan include infrastructure/environment provisioning? →
      A: No. The approved specification explicitly excludes Azure
      deployment, provisioning, and any cloud resource (Exclusions; D8;
      NFR-5). No infrastructure task is included.
- [x] Q: Should the plan include integration testing against migrated Azure
      services? → A: Not applicable. This change migrates no external
      service to Azure; local automated tests covering AC-1 to AC-10 are
      instead delivered inside the upgrade task's new `tests/` project per
      decision Q2, not as a separate Azure integration-test task.
- [x] Q: Should the plan include a security/CVE remediation task? → A: Yes
      (default) — scan and remediate vulnerable NuGet dependencies after the
      upgrade, scoped to the source root only.
- [x] Q: Which Azure deployment target should the plan use? → A: No
      deployment. The specification states "No deployment is authorized"
      and Azure assets under `legacy-src/dotnet-bookcatalog/examples/azure/`
      require separate approval outside this specification.
- [ ] The user's broader instruction asked to "prepare optional Azure-ready
      configuration without deploying." The approved specification
      (Exclusions; D8) treats any Azure-facing configuration as out of
      scope until a separate, explicitly approved change. This plan follows
      the approved specification and does not add Azure-readiness tasks;
      revisit only via a new approved specification.
