using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Web.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; } = null!;
    }
}
