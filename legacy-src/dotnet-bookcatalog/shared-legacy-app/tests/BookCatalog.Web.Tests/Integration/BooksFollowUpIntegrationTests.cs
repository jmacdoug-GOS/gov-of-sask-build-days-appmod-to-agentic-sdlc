using System.Net;

namespace BookCatalog.Web.Tests.Integration;

// Follow-up integration tests; reuses the helpers and the disposable BookCatalogTests_<guid> database.
public sealed partial class BooksIntegrationTests
{
    [Theory]
    [InlineData("", "Author", "", "The Title field is required.")]
    [InlineData("Valid Title", "", "", "The Author field is required.")]
    [InlineData("TOOLONG", "Author", "", "The field Title must be a string with a maximum length of 200.")]
    [InlineData("Valid Title", "Author", "12345678901234", "The field ISBN must be a string with a maximum length of 13.")]
    public async Task Edit_Invalid_RedisplaysFormWithValidationMessage_AndChangesNothing(
        string title, string author, string isbn, string expectedMessage)
    {
        var client = StartHost();
        await client.GetAsync("/");
        var original = ReadBooks().First();
        var token = await GetTokenAsync(client, $"/Books/Edit/{original.Id}");

        var fields = BookFields(title == "TOOLONG" ? new string('t', 201) : title, author);
        fields["ISBN"] = isbn;
        fields["Id"] = original.Id.ToString();
        var response = await PostFormAsync(client, $"/Books/Edit/{original.Id}", fields, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(expectedMessage, html);
        Assert.Contains("class=\"error-box\" style=\"display:block\"", html);
        Assert.Contains("Edit Book", html);

        var saved = ReadBooks().Single(b => b.Id == original.Id);
        Assert.Equal(original.Title, saved.Title);
        Assert.Equal(original.Author, saved.Author);
        Assert.Equal(original.ISBN, saved.ISBN);
    }

    [Fact]
    public async Task Create_IgnoresForgedIdAndCreatedDate_ButHonoursIsActive()
    {
        var client = StartHost();
        await client.GetAsync("/");
        var seeded = ReadBooks();
        var existing = seeded.First();
        var before = DateTime.Now;
        var token = await GetTokenAsync(client, "/Books/Create");

        var fields = BookFields("Forged Fields Book");
        fields["IsActive"] = "false";
        fields["Id"] = existing.Id.ToString();
        fields["CreatedDate"] = "1990-01-01T00:00:00";
        var response = await PostFormAsync(client, "/Books/Create", fields, token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var books = ReadBooks();
        Assert.Equal(seeded.Count + 1, books.Count);

        var created = books.Single(b => b.Title == "Forged Fields Book");
        Assert.NotEqual(existing.Id, created.Id);
        Assert.InRange(created.CreatedDate, before.AddSeconds(-1), DateTime.Now.AddSeconds(1));
        Assert.False(created.IsActive);

        var untouched = books.Single(b => b.Id == existing.Id);
        Assert.Equal(existing.Title, untouched.Title);
        Assert.Equal(existing.CreatedDate, untouched.CreatedDate);
    }

    [Fact]
    public async Task Edit_IgnoresForgedCreatedDate()
    {
        var client = StartHost();
        await client.GetAsync("/");
        var original = ReadBooks().First();
        var token = await GetTokenAsync(client, $"/Books/Edit/{original.Id}");

        var fields = BookFields("Edited Title");
        fields["Id"] = original.Id.ToString();
        fields["CreatedDate"] = "1990-01-01T00:00:00";
        var response = await PostFormAsync(client, $"/Books/Edit/{original.Id}", fields, token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var saved = ReadBooks().Single(b => b.Id == original.Id);
        Assert.Equal("Edited Title", saved.Title);
        Assert.Equal(original.CreatedDate, saved.CreatedDate);
    }

    [Fact]
    public async Task Index_ShowsUserAgentDiagnostic_OnlyWhenHeaderPresent()
    {
        var client = StartHost();

        var without = await client.GetStringAsync("/Books");
        Assert.DoesNotContain("YOUR BROWSER:", without);
        Assert.DoesNotContain("user-agent-box", without);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/Books");
        request.Headers.TryAddWithoutValidation("User-Agent", "SyntheticAgent/1.0");
        var with = await (await client.SendAsync(request)).Content.ReadAsStringAsync();
        Assert.Contains("YOUR BROWSER:", with);
        Assert.Contains("SyntheticAgent/1.0", with);
    }

    [Fact]
    public async Task Layout_ShowsNewPlatformFooter_AndKeepsLegacyElements()
    {
        var client = StartHost();

        var html = await client.GetStringAsync("/Books");

        Assert.Contains("ASP.NET Core MVC", html);
        Assert.Contains("Entity Framework Core", html);
        Assert.Contains(".NET 10", html);
        Assert.DoesNotContain("MVC 5", html);
        Assert.DoesNotContain("Entity Framework 6", html);
        Assert.DoesNotContain(".NET Framework 4.8", html);
        Assert.Contains("001337", html);
        Assert.Contains("Page generated in 0.042 seconds", html);
        Assert.Contains("Internet Explorer 6.0+", html);
    }
}
