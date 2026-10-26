using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class AssistantController(
    TenantAccessService tenantAccess,
    FeatureGateService features,
    AssistantService assistant) : Controller
{
    public async Task<IActionResult> Index(Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await tenantAccess.AccessibleWorkspaces.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == workspaceId, cancellationToken);
        if (workspace is null) return NotFound();
        if (!features.IsEnabled(workspace.Plan, WorkspaceFeature.AiAssistant)) return Forbid();
        return View(workspace);
    }

    [HttpGet("api/assistant/stream")]
    public async Task Stream(Guid workspaceId, string message, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        var workspace = await tenantAccess.AccessibleWorkspaces.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == workspaceId, cancellationToken);
        if (workspace is null || !features.IsEnabled(workspace.Plan, WorkspaceFeature.AiAssistant))
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            await WriteEventAsync("error", "Доступ до робочого простору заборонено.", cancellationToken);
            return;
        }

        await WriteEventAsync("step", "Пошук документів робочого простору", cancellationToken);
        var documents = await tenantAccess.AccessibleDocuments.AsNoTracking()
            .Where(document => document.WorkspaceId == workspaceId)
            .OrderByDescending(document => document.UpdatedAt)
            .Select(document => new SearchDocument(document.Id, document.Title, document.Summary, workspace.Name))
            .ToListAsync(cancellationToken);
        await WriteEventAsync("tool", $"Пошук знайшов документів: {documents.Count}", cancellationToken);
        var answer = await assistant.AnswerAsync(message.Trim(), documents, cancellationToken);
        foreach (var chunk in answer.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            await WriteEventAsync("token", chunk + " ", cancellationToken);
            await Task.Delay(25, cancellationToken);
        }
        await WriteEventAsync("done", "", cancellationToken);
    }

    private async Task WriteEventAsync(string type, string value, CancellationToken cancellationToken)
    {
        await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type, value })}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
