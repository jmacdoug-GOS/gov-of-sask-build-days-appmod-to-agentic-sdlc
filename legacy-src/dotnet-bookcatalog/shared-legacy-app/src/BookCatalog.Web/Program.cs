using BookCatalog.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BookCatalogContext")));

var app = builder.Build();

// Site.css lives in Content/ (not wwwroot) and keeps its /Content/Site.css URL.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Content")),
    RequestPath = "/Content"
});

app.UseRouting();

app.MapControllerRoute(
    name: "Default",
    pattern: "{controller=Books}/{action=Index}/{id?}");

app.Run();
