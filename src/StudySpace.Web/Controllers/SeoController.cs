using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Data;

namespace StudySpace.Web.Controllers;

public sealed class SeoController(StudySpaceDbContext dbContext) : Controller
{
    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var sitemapUrl = Url.ActionLink(nameof(Sitemap), "Seo") ?? "/sitemap.xml";
        return Content($"User-agent: *\nAllow: /\nSitemap: {sitemapUrl}\n", "text/plain");
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 900)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var root = new XElement(ns + "urlset",
            CreateEntry(ns, Url.ActionLink("Index", "Home")!),
            CreateEntry(ns, Url.ActionLink("Index", "Workspaces")!),
            CreateEntry(ns, Url.ActionLink("Index", "Insights")!));

        var documents = await dbContext.Documents
            .AsNoTracking()
            .OrderBy(document => document.Id)
            .Select(document => new { document.Id, document.UpdatedAt })
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            root.Add(CreateEntry(
                ns,
                Url.ActionLink("Details", "Documents", new { document.Id })!,
                document.UpdatedAt));
        }

        var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        return Content(xml.ToString(), "application/xml");
    }

    private static XElement CreateEntry(XNamespace ns, string location, DateTime? modified = null) =>
        new(ns + "url",
            new XElement(ns + "loc", location),
            modified.HasValue
                ? new XElement(ns + "lastmod", modified.Value.ToString("yyyy-MM-dd"))
                : null);
}

