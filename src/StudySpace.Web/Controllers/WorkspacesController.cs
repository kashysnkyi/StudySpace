using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;
using StudySpace.Data;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class WorkspacesController(
    StudySpaceDbContext dbContext,
    TenantAccessService tenantAccess,
    CurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var workspaces = await tenantAccess.AccessibleWorkspaces
            .AsNoTracking()
            .Include(workspace => workspace.Documents)
            .OrderBy(workspace => workspace.Name)
            .ToListAsync(cancellationToken);

        return View(workspaces);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await tenantAccess.AccessibleWorkspaces
            .AsNoTracking()
            .Include(item => item.Documents.OrderByDescending(document => document.UpdatedAt))
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return workspace is null ? NotFound() : View(workspace);
    }

    public IActionResult Create() => View(new Workspace());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Workspace workspace, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(workspace);
        }

        workspace.Members.Add(new WorkspaceMember
        {
            Subject = currentUser.Subject,
            DisplayName = currentUser.DisplayName,
            Role = WorkspaceRole.Owner
        });
        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = $"Робочий простір «{workspace.Name}» створено.";
        return RedirectToAction(nameof(Details), new { workspace.Id });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await tenantAccess.AccessibleWorkspaces
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (workspace is not null && !await tenantAccess.CanEditWorkspaceAsync(id, cancellationToken))
        {
            return Forbid();
        }
        return workspace is null ? NotFound() : View(workspace);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Workspace workspace, CancellationToken cancellationToken)
    {
        if (id != workspace.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(workspace);
        }

        if (!await tenantAccess.CanEditWorkspaceAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var storedWorkspace = await tenantAccess.AccessibleWorkspaces
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (storedWorkspace is null)
        {
            return NotFound();
        }

        storedWorkspace.Name = workspace.Name;
        storedWorkspace.Description = workspace.Description;
        storedWorkspace.Plan = workspace.Plan;
        storedWorkspace.Latitude = workspace.Latitude;
        storedWorkspace.Longitude = workspace.Longitude;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Відомості про робочий простір оновлено.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await tenantAccess.AccessibleWorkspaces
            .AsNoTracking()
            .Include(item => item.Documents)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return workspace is null ? NotFound() : View(workspace);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditWorkspaceAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var workspace = await tenantAccess.AccessibleWorkspaces
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (workspace is null)
        {
            return NotFound();
        }

        dbContext.Workspaces.Remove(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Робочий простір видалено.";
        return RedirectToAction(nameof(Index));
    }
}

