using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;
using StudySpace.Web.Models;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public class HomeController(TenantAccessService tenantAccess) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var recentDocuments = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .Include(document => document.Workspace)
            .OrderByDescending(document => document.UpdatedAt)
            .Take(3)
            .ToListAsync(cancellationToken);

        var model = new DashboardViewModel(
            await tenantAccess.AccessibleWorkspaces.CountAsync(cancellationToken),
            await tenantAccess.AccessibleDocuments.CountAsync(cancellationToken),
            await tenantAccess.AccessibleDocuments.CountAsync(document => document.Status == DocumentStatus.Published, cancellationToken),
            await tenantAccess.AccessibleDocuments.SelectMany(document => document.Attachments).CountAsync(cancellationToken),
            recentDocuments);

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
