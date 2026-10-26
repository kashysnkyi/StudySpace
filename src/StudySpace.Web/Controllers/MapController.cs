using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class MapController(TenantAccessService tenantAccess) : Controller
{
    public IActionResult Index() => View();

    [HttpGet("api/map/workspaces")]
    public async Task<IActionResult> Workspaces(string? plan, CancellationToken cancellationToken)
    {
        var query = tenantAccess.AccessibleWorkspaces.AsNoTracking()
            .Where(workspace => workspace.Latitude != null && workspace.Longitude != null);

        if (Enum.TryParse<StudySpace.Core.Entities.SubscriptionPlan>(plan, true, out var parsedPlan))
        {
            query = query.Where(workspace => workspace.Plan == parsedPlan);
        }

        return Ok(await query.OrderBy(workspace => workspace.Name).Select(workspace => new
        {
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.Plan,
            workspace.Latitude,
            workspace.Longitude,
            DocumentCount = workspace.Documents.Count,
            Url = Url.Action("Details", "Workspaces", new { workspace.Id })
        }).ToListAsync(cancellationToken));
    }
}
