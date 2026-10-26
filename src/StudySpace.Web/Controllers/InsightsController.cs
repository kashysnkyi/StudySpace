using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Data;
using StudySpace.Web.Models;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class InsightsController(
    StudySpaceDbContext dbContext,
    TenantAccessService tenantAccess,
    CurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var statusCounts = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .GroupBy(document => document.Status)
            .Select(group => new { Status = group.Key.ToString(), Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        var model = new InsightsViewModel(
            await tenantAccess.AccessibleWorkspaces.CountAsync(cancellationToken),
            await tenantAccess.AccessibleDocuments.CountAsync(cancellationToken),
            await dbContext.DocumentBlocks.CountAsync(block => block.Document!.Workspace!.Members.Any(
                member => member.Subject == currentUser.Subject), cancellationToken),
            await dbContext.Attachments.CountAsync(attachment => attachment.Document!.Workspace!.Members.Any(
                member => member.Subject == currentUser.Subject), cancellationToken),
            statusCounts);

        return View(model);
    }
}

