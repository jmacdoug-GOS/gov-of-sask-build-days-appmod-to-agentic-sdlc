using System.Net;
using System.Text.RegularExpressions;
using BookCatalog.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Web.Tests.Integration;

// Integration tests: real host, EF Core migrations and a uniquely named disposable LocalDB database.
[Trait("Category", "Integration")]
public sealed partial class BooksIntegrationTests : IDisposable
{
    private readonly string databaseName = TestDatabase.NewName();
    private readonly List<BookCatalogWebFactory> factories = new();

    public void Dispose()
    {
        StopHosts();
        TestDatabase.Drop(databaseName);
    }

    private HttpClient StartHost()
    {
        var factory = new BookCatalogWebFactory(databaseName);
        factories.Add(factory);
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private void StopHosts()
    {
        foreach (var factory in factories)
        {
            factory.Dispose();
        }

        factories.Clear();
    }

    private List<Book> ReadBooks()
    {
        using var context = TestDatabase.OpenContext(databaseName);
        return context.Books.AsNoTracking().OrderBy(b => b.Id).ToList();
    }

    [GeneratedRegex("__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    private static async Task<string> GetTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = TokenPattern().Match(html);
        Assert.True(match.Success, $"No antiforgery token on {url}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string url, Dictionary<string, string> fields, string? token)
    {
        if (token != null)
        {
            fields["__RequestVerificationToken"] = token;
        }

        return await client.PostAsync(url, new FormUrlEncodedContent(fields));
    }

    private static Dictionary<string, string> BookFields(string title, string author = "Synthetic Author", string year = "") => new()
    {
        ["Title"] = title,
        ["Author"] = author,
        ["ISBN"] = "",
        ["PublishedYear"] = year,
        ["IsActive"] = "true"
    };

    private async Task<Book> CreateBookAsync(HttpClient client, string title)
    {
        var token = await GetTokenAsync(client, "/Books/Create");
        var response = await PostFormAsync(client, "/Books/Create", BookFields(title), token);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return ReadBooks().Single(b => b.Title == title);
    }

    [Fact]
    public async Task FirstStart_SeedsSevenBooks_AndListsSixActiveInTitleOrder()
    {
        var client = StartHost();

        foreach (var url in new[] { "/", "/Books" })
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();

            Assert.Contains("Showing 6 record(s)", html);
            Assert.DoesNotContain("The Matrix", html);
            var order = new[] { "Clean Code", "Design Patterns", "Jurassic Park", "Hitchhiker", "The Lord of the Rings", "The Pragmatic Programmer" }
                .Select(t => html.IndexOf(t, StringComparison.Ordinal)).ToArray();
            Assert.All(order, i => Assert.True(i >= 0));
            Assert.Equal(order.OrderBy(i => i), order);
        }

        var books = ReadBooks();
        Assert.Equal(7, books.Count);
        Assert.Equal(6, books.Count(b => b.IsActive));
        Assert.False(books.Single(b => b.Title.StartsWith("The Matrix", StringComparison.Ordinal)).IsActive);
        Assert.Equal(new DateTime(2001, 3, 15), books.Single(b => b.Title.StartsWith("The Hitchhiker", StringComparison.Ordinal)).CreatedDate);
    }

    [Fact]
    public async Task SecondStart_DoesNotReseedAPopulatedDatabase()
    {
        var first = StartHost();
        await first.GetAsync("/");
        Assert.Equal(7, ReadBooks().Count);

        using (var context = TestDatabase.OpenContext(databaseName))
        {
            context.Books.Remove(context.Books.First(b => b.IsActive));
            context.SaveChanges();
        }

        StopHosts();
        var second = StartHost();
        await second.GetAsync("/");

        Assert.Equal(6, ReadBooks().Count);
    }

    [Theory]
    [InlineData("/Books/Create")]
    [InlineData("/Books/Edit/1")]
    [InlineData("/Books/Delete/1")]
    public async Task PostWithoutAntiforgeryToken_IsRejectedAndChangesNothing(string url)
    {
        var client = StartHost();
        await client.GetAsync("/");
        var before = ReadBooks();

        var fields = BookFields("Should Not Save");
        fields["Id"] = "1";
        var response = await PostFormAsync(client, url, fields, token: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var after = ReadBooks();
        Assert.Equal(before.Select(b => (b.Id, b.Title)), after.Select(b => (b.Id, b.Title)));
    }

    [Fact]
    public async Task UnknownId_Returns404_AndDeletePostRedirects()
    {
        var client = StartHost();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Books/Details/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Books/Edit/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Books/Delete/999999")).StatusCode);

        var existing = ReadBooks().First();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/Books/Details/{existing.Id}")).StatusCode);

        var token = await GetTokenAsync(client, $"/Books/Edit/{existing.Id}");
        var editFields = BookFields("Ghost");
        editFields["Id"] = "999999";
        Assert.Equal(HttpStatusCode.NotFound, (await PostFormAsync(client, "/Books/Edit/999999", editFields, token)).StatusCode);

        var deleteToken = await GetTokenAsync(client, $"/Books/Delete/{existing.Id}");
        var deleteResponse = await PostFormAsync(client, "/Books/Delete/999999", new Dictionary<string, string> { ["Id"] = "999999" }, deleteToken);
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Create_Valid_SavesWithLocalCreatedDate_AndRedirectsToList()
    {
        var client = StartHost();
        await client.GetAsync("/");
        var before = DateTime.Now;

        var created = await CreateBookAsync(client, "Integration Created Book");

        Assert.InRange(created.CreatedDate, before.AddSeconds(-1), DateTime.Now.AddSeconds(1));
        Assert.True(created.IsActive);
        Assert.Equal(8, ReadBooks().Count);
    }

    [Theory]
    [InlineData("", "Author", "")]
    [InlineData("Title", "", "")]
    [InlineData("Title", "Author", "1799")]
    [InlineData("Title", "Author", "2101")]
    public async Task Create_Invalid_RedisplaysFormAndSavesNothing(string title, string author, string year)
    {
        var client = StartHost();
        await client.GetAsync("/");
        var token = await GetTokenAsync(client, "/Books/Create");

        var response = await PostFormAsync(client, "/Books/Create", BookFields(title, author, year), token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(7, ReadBooks().Count);
    }

    [Fact]
    public async Task Edit_PersistsAcrossRestart_AndKeepsCreatedDate()
    {
        var client = StartHost();
        await client.GetAsync("/");
        var created = await CreateBookAsync(client, "Before Restart Book");

        var token = await GetTokenAsync(client, $"/Books/Edit/{created.Id}");
        var fields = BookFields("After Restart Book", "Edited Author", "2000");
        fields["Id"] = created.Id.ToString();
        var response = await PostFormAsync(client, $"/Books/Edit/{created.Id}", fields, token);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        StopHosts();
        var restarted = StartHost();
        var details = await restarted.GetAsync($"/Books/Details/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Contains("After Restart Book", await details.Content.ReadAsStringAsync());
        var saved = ReadBooks().Single(b => b.Id == created.Id);
        Assert.Equal("After Restart Book", saved.Title);
        Assert.Equal("Edited Author", saved.Author);
        Assert.Equal(2000, saved.PublishedYear);
        Assert.Equal(created.CreatedDate, saved.CreatedDate);
        Assert.Equal(8, ReadBooks().Count);
    }

    [Fact]
    public async Task Delete_PermanentlyRemovesTheRecord()
    {
        var client = StartHost();
        await client.GetAsync("/");
        var created = await CreateBookAsync(client, "Delete Me Book");

        var token = await GetTokenAsync(client, $"/Books/Delete/{created.Id}");
        var response = await PostFormAsync(client, $"/Books/Delete/{created.Id}", new Dictionary<string, string> { ["Id"] = created.Id.ToString() }, token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain(ReadBooks(), b => b.Id == created.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Books/Details/{created.Id}")).StatusCode);
    }
}
