using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class BoardController(TenantAccessService tenantAccess, FeatureGateService features) : Controller
{
    public async Task<IActionResult> Index(Guid? workspaceId, CancellationToken cancellationToken)
    {
        var workspace = workspaceId.HasValue
            ? await tenantAccess.AccessibleWorkspaces.AsNoTracking().SingleOrDefaultAsync(item => item.Id == workspaceId, cancellationToken)
            : await tenantAccess.AccessibleWorkspaces.AsNoTracking().OrderBy(item => item.Name).FirstOrDefaultAsync(cancellationToken);
        if (workspace is null)
        {
            return NotFound();
        }
        if (!features.IsEnabled(workspace.Plan, WorkspaceFeature.SharedBoard))
        {
            TempData["Error"] = "Спільна дошка доступна в тарифі Преміум.";
            return RedirectToAction("Details", "Workspaces", new { id = workspace.Id });
        }

        ViewBag.BoardId = workspace.Id.ToString("N");
        ViewBag.WorkspaceName = workspace.Name;
        return View();
    }
}

