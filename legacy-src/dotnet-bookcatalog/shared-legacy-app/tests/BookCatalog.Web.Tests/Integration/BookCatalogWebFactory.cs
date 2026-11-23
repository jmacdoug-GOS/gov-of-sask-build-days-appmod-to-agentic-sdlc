using BookCatalog.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Web.Tests.Integration;

// Disposable LocalDB database naming; the sample lab database is never used by tests.
public static class TestDatabase
{
    private const string Prefix = "BookCatalogTests_";

    public static string NewName() => Prefix + Guid.NewGuid().ToString("N");

    public static string ConnectionString(string name)
    {
        EnsureDisposableName(name);
        return $"Server=(localdb)\\MSSQLLocalDB;Initial Catalog={name};Integrated Security=True;TrustServerCertificate=True";
    }

    public static ApplicationDbContext OpenContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString(name))
            .Options;
        return new ApplicationDbContext(options);
    }

    public static void Drop(string name)
    {
        using var context = OpenContext(name);
        context.Database.EnsureDeleted();
    }

    private static void EnsureDisposableName(string name)
    {
        if (!name.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Tests may only use databases named {Prefix}*.");
        }
    }
}

public sealed class BookCatalogWebFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString;

    public BookCatalogWebFactory(string databaseName)
    {
        connectionString = TestDatabase.ConnectionString(databaseName);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:BookCatalogContext", connectionString);
    }
}
