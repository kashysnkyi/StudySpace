using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class FilesController(TenantAccessService tenantAccess) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var documents = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .Include(document => document.Workspace)
            .Include(document => document.Attachments.OrderByDescending(attachment => attachment.Id))
            .OrderBy(document => document.Workspace!.Name)
            .ThenBy(document => document.Title)
            .ToListAsync(cancellationToken);

        return View(documents);
    }
}
