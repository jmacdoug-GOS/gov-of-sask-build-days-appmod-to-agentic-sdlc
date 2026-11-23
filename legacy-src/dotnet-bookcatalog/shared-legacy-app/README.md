# Run the legacy BookCatalog sample

<!-- repo-only:start -->
> [!TIP]
> **Prefer the web experience?** [Open this page on the course website](https://microsoft.github.io/dotnet-modernization-for-beginners/#/reference?path=shared-legacy-app%2FREADME.md) for the best reading experience and navigation.
<!-- repo-only:end -->

BookCatalog is an ASP.NET Core MVC application on .NET 10 using EF Core and SQL Server LocalDB.

Its users add, inspect, edit, and remove book records. The main list hides inactive books and sorts active books by title.

You can run this sample without completing the course. The modernization course is maintained in the
[upstream repository](https://github.com/microsoft/dotnet-modernization-for-beginners/tree/82585e280fe00d0c56a075bf623dad1622c15911).

## Windows quickstart

You need the .NET 10 SDK and `MSSQLLocalDB`.

> **Sample data only.** The app uses the local `BookCatalogModernizedLab` database. EF Core migrations are applied at startup and the seed runs only when the `Books` table is empty. The database is never dropped or recreated on start.
>
> Never change the connection string to a business database. Do not create an empty MDF.

1. From `legacy-src\dotnet-bookcatalog\shared-legacy-app`, run `dotnet build BookCatalog.sln -c Release`.
2. Run `dotnet run --project src\BookCatalog.Web\BookCatalog.Web.csproj`.
3. Open the URL printed by the command.
4. Check that the active catalog loads.

The first start creates `BookCatalogModernizedLab` on `(localdb)\MSSQLLocalDB` and seeds it. Later starts keep existing data.

A fresh database contains six active books and one inactive Matrix record. Existing sample data can change those counts.

Try adding an active sample book and editing it. Select **Active** explicitly when creating it.

The [upstream setup app tour](https://github.com/microsoft/dotnet-modernization-for-beginners/blob/82585e280fe00d0c56a075bf623dad1622c15911/03-prerequisites/README.md#run-bookcatalog) covers ordinary use.
The [detailed behavior checklist](https://github.com/microsoft/dotnet-modernization-for-beginners/blob/82585e280fe00d0c56a075bf623dad1622c15911/docs/learner-record.md#behavior-checks) is optional.

The connection string is in `src\BookCatalog.Web\appsettings.json`. It uses integrated authentication to LocalDB and does not contain a SQL password.

If startup fails, inspect the first error and check that `MSSQLLocalDB` is available. Don't create an empty MDF to fix a connection error. To reset the sample data, drop the `BookCatalogModernizedLab` database yourself; the app will not do it.

## Working through the course

For the full modernization exercise, start with the [upstream introduction](https://github.com/microsoft/dotnet-modernization-for-beginners/tree/82585e280fe00d0c56a075bf623dad1622c15911/02-introduction).

Chapters 04 and 05 use the modernization agent through Copilot Chat in Visual Studio to assess the app and generate its upgrade plan.

For modernization, use a stable .NET SDK 10 or later. The upgraded app still targets .NET 10, even with a later SDK.

Keep the .NET 10 runtime component installed through Visual Studio Installer's **Modify > Individual components** page.

Chapter 06 upgrades **your copy** in place to ASP.NET Core MVC on .NET 10 with EF Core.

The plan replaces the old web host. It doesn't keep a side-by-side host, shared schema, YARP, or System.Web adapters.

The data is disposable. EF Core migrations create the schema and the startup seed populates `BookCatalogModernizedLab`.

Don't reset it on every startup. Add a book, edit it, and restart the upgraded app to confirm that the saved edit remains.

Existing records don't need to survive modernization. [Data transfer](https://github.com/microsoft/dotnet-modernization-for-beginners/blob/82585e280fe00d0c56a075bf623dad1622c15911/docs/data-transfer.md) is a standalone reference, not a course prerequisite.

The optional Azure deployment assets are under `legacy-src\dotnet-bookcatalog\examples\azure` and need separate access and cost approval.

For comparison, inspect the original Git commit or another clone. Do not reset over your work.
