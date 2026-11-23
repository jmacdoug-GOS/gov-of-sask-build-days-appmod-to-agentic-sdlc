using System.Reflection;
using BookCatalog.Web.Controllers;
using BookCatalog.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Web.Tests.Unit;

// Uses the EF Core in-memory provider only; no LocalDB.
public sealed class BooksControllerTests : IDisposable
{
    private readonly ApplicationDbContext db;
    private readonly BooksController controller;

    public BooksControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new ApplicationDbContext(options);
        controller = new BooksController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    public void Dispose()
    {
        controller.Dispose();
        db.Dispose();
    }

    private Book AddBook(string title, bool active = true, DateTime? created = null)
    {
        var book = new Book
        {
            Title = title,
            Author = "Synthetic Author",
            IsActive = active,
            CreatedDate = created ?? new DateTime(2001, 1, 1)
        };
        db.Books.Add(book);
        db.SaveChanges();
        return book;
    }

    [Fact]
    public void Index_ListsOnlyActiveBooksOrderedByTitle()
    {
        AddBook("Charlie");
        AddBook("Alpha");
        AddBook("Hidden", active: false);
        AddBook("Bravo");

        var result = Assert.IsType<ViewResult>(controller.Index());

        var model = Assert.IsAssignableFrom<IEnumerable<Book>>(result.Model);
        Assert.Equal(new[] { "Alpha", "Bravo", "Charlie" }, model.Select(b => b.Title));
    }

    [Fact]
    public void Index_ExposesUserAgentOnlyWhenPresent()
    {
        controller.Index();
        Assert.Null(controller.ViewData["UserAgent"]);

        controller.Request.Headers.UserAgent = "SyntheticAgent/1.0";
        controller.Index();
        Assert.Equal("SyntheticAgent/1.0", controller.ViewData["UserAgent"]);
    }

    [Fact]
    public void UnknownId_Returns404ForDetailsEditAndDelete()
    {
        Assert.IsType<NotFoundResult>(controller.Details(999));
        Assert.IsType<NotFoundResult>(controller.Edit(999));
        Assert.IsType<NotFoundResult>(controller.Delete(999));
        Assert.IsType<NotFoundResult>(controller.Edit(new Book { Id = 999, Title = "x", Author = "y" }));
    }

    [Fact]
    public void Details_ForExistingBook_ReturnsIt()
    {
        var book = AddBook("Existing");

        var result = Assert.IsType<ViewResult>(controller.Details(book.Id));

        Assert.Same(book, result.Model);
    }

    [Fact]
    public void Create_Valid_SetsLocalCreatedDateAndRedirectsToList()
    {
        var before = DateTime.Now;

        var result = controller.Create(new Book { Title = "New", Author = "Author", CreatedDate = new DateTime(1990, 1, 1) });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        var saved = Assert.Single(db.Books);
        Assert.InRange(saved.CreatedDate, before, DateTime.Now);
    }

    [Fact]
    public void Create_Invalid_RedisplaysFormAndSavesNothing()
    {
        controller.ModelState.AddModelError("Title", "The Title field is required.");

        var result = controller.Create(new Book { Author = "Author" });

        Assert.IsType<ViewResult>(result);
        Assert.Empty(db.Books);
    }

    [Fact]
    public void Edit_Valid_UpdatesAllowedFieldsAndKeepsCreatedDate()
    {
        var created = new DateTime(2005, 5, 5);
        var book = AddBook("Before", created: created);

        var result = controller.Edit(new Book
        {
            Id = book.Id,
            Title = "After",
            Author = "New Author",
            ISBN = "1234567890123",
            PublishedYear = 1999,
            IsActive = false,
            CreatedDate = new DateTime(1990, 1, 1)
        });

        Assert.IsType<RedirectToActionResult>(result);
        var saved = db.Books.Single();
        Assert.Equal("After", saved.Title);
        Assert.Equal("New Author", saved.Author);
        Assert.Equal("1234567890123", saved.ISBN);
        Assert.Equal(1999, saved.PublishedYear);
        Assert.False(saved.IsActive);
        Assert.Equal(created, saved.CreatedDate);
    }

    [Fact]
    public void Edit_Invalid_RedisplaysFormAndChangesNothing()
    {
        var book = AddBook("Original");
        controller.ModelState.AddModelError("Author", "The Author field is required.");

        var result = controller.Edit(new Book { Id = book.Id, Title = "Changed", Author = "" });

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Original", db.Books.Single().Title);
    }

    [Fact]
    public void DeleteConfirmed_RemovesTheRecordPermanently()
    {
        var book = AddBook("Doomed");

        var result = controller.DeleteConfirmed(book.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(db.Books);
    }

    [Fact]
    public void DeleteConfirmed_ForMissingId_RedirectsToList()
    {
        var result = controller.DeleteConfirmed(999);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Theory]
    [InlineData(nameof(BooksController.Create))]
    [InlineData(nameof(BooksController.Edit))]
    [InlineData(nameof(BooksController.DeleteConfirmed))]
    public void StateChangingPostActions_RequireAntiforgeryToken(string actionName)
    {
        var postAction = typeof(BooksController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == actionName && m.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.NotNull(postAction.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }
}
