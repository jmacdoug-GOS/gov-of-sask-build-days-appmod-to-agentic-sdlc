using System.ComponentModel.DataAnnotations;
using BookCatalog.Web.Models;

namespace BookCatalog.Web.Tests.Unit;

public class BookValidationTests
{
    private static bool IsValid(Book book, out List<ValidationResult> results)
    {
        results = new List<ValidationResult>();
        return Validator.TryValidateObject(book, new ValidationContext(book), results, validateAllProperties: true);
    }

    private static Book ValidBook() => new() { Title = "Synthetic Title", Author = "Synthetic Author" };

    [Fact]
    public void NewBook_DefaultsToActiveWithLocalCreatedDate()
    {
        var before = DateTime.Now;
        var book = new Book();
        var after = DateTime.Now;

        Assert.True(book.IsActive);
        Assert.InRange(book.CreatedDate, before, after);
    }

    [Theory]
    [InlineData(200, true)]
    [InlineData(201, false)]
    public void Title_LengthBoundary(int length, bool expected)
    {
        var book = ValidBook();
        book.Title = new string('t', length);
        Assert.Equal(expected, IsValid(book, out _));
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void Author_LengthBoundary(int length, bool expected)
    {
        var book = ValidBook();
        book.Author = new string('a', length);
        Assert.Equal(expected, IsValid(book, out _));
    }

    [Theory]
    [InlineData(13, true)]
    [InlineData(14, false)]
    public void Isbn_LengthBoundary(int length, bool expected)
    {
        var book = ValidBook();
        book.ISBN = new string('1', length);
        Assert.Equal(expected, IsValid(book, out _));
    }

    [Fact]
    public void Isbn_IsOptional()
    {
        var book = ValidBook();
        book.ISBN = null;
        Assert.True(IsValid(book, out _));
    }

    [Theory]
    [InlineData(1799, false)]
    [InlineData(1800, true)]
    [InlineData(2100, true)]
    [InlineData(2101, false)]
    public void PublishedYear_RangeBoundary(int year, bool expected)
    {
        var book = ValidBook();
        book.PublishedYear = year;
        Assert.Equal(expected, IsValid(book, out _));
    }

    [Fact]
    public void PublishedYear_IsOptional()
    {
        var book = ValidBook();
        book.PublishedYear = null;
        Assert.True(IsValid(book, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Title_IsRequired(string? title)
    {
        var book = ValidBook();
        book.Title = title!;
        Assert.False(IsValid(book, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Title)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Author_IsRequired(string? author)
    {
        var book = ValidBook();
        book.Author = author!;
        Assert.False(IsValid(book, out var results));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Author)));
    }
}
