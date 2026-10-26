using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Data;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers.Api;

[ApiController]
[Route("api/search")]
public sealed class SearchApiController(TenantAccessService tenantAccess, DocumentSearchService searchService) : ControllerBase
{
    [HttpGet("documents")]
    public async Task<IActionResult> Documents([FromQuery] string? term, CancellationToken cancellationToken)
    {
        var normalized = term?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < 3)
        {
            return Ok(Array.Empty<object>());
        }

        var source = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .Select(document => new SearchDocument(document.Id, document.Title, document.Summary, document.Workspace!.Name))
            .ToListAsync(cancellationToken);

        var matches = searchService.Search(source, normalized).Select(document => new
        {
            document.Id,
            document.Title,
            document.Workspace,
            document.Score,
            Url = Url.Action("Details", "Documents", new { document.Id })
        });

        return Ok(matches);
    }
}

