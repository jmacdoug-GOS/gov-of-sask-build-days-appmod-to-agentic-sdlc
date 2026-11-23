using BookCatalog.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookCatalog.Web.Controllers
{
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext db;

        public BooksController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public IActionResult Index()
        {
            var books = db.Books.Where(b => b.IsActive).OrderBy(b => b.Title).ToList();

            var userAgent = Request.Headers.UserAgent;
            ViewBag.UserAgent = userAgent.Count > 0 ? userAgent.ToString() : null;

            return View(books);
        }

        public IActionResult Details(int id)
        {
            var book = db.Books.Find(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Title,Author,ISBN,PublishedYear,IsActive")] Book book)
        {
            if (ModelState.IsValid)
            {
                book.CreatedDate = DateTime.Now;
                db.Books.Add(book);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(book);
        }

        public IActionResult Edit(int id)
        {
            var book = db.Books.Find(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit([Bind("Id,Title,Author,ISBN,PublishedYear,IsActive")] Book book)
        {
            if (ModelState.IsValid)
            {
                var existingBook = db.Books.Find(book.Id);
                if (existingBook == null)
                {
                    return NotFound();
                }

                existingBook.Title = book.Title;
                existingBook.Author = book.Author;
                existingBook.ISBN = book.ISBN;
                existingBook.PublishedYear = book.PublishedYear;
                existingBook.IsActive = book.IsActive;

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(book);
        }

        public IActionResult Delete(int id)
        {
            var book = db.Books.Find(id);
            if (book == null)
            {
                return NotFound();
            }
            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var book = db.Books.Find(id);
            if (book != null)
            {
                db.Books.Remove(book);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
