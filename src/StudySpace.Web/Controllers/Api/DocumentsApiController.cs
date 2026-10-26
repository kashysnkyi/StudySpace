using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudySpace.Core.Entities;
using StudySpace.Data;
using StudySpace.Web.Contracts.Documents;
using StudySpace.Web.Services;
using Microsoft.ApplicationInsights;

namespace StudySpace.Web.Controllers.Api;

[ApiController]
[Route("api/documents")]
[Produces("application/json")]
public sealed class DocumentsApiController(
    StudySpaceDbContext dbContext,
    IMemoryCache cache,
    TenantAccessService tenantAccess,
    CurrentUserService currentUser,
    WebhookNotificationService notifications,
    TelemetryClient telemetry,
    IActivityQueue activityQueue) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<DocumentSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DocumentSummaryResponse>>> GetAll(
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 100)] int limit = 20,
        [FromQuery] Guid? workspaceId = null,
        [FromQuery] string? query = null,
        CancellationToken cancellationToken = default)
    {
        var documents = tenantAccess.AccessibleDocuments.AsNoTracking();

        if (workspaceId.HasValue)
        {
            documents = documents.Where(document => document.WorkspaceId == workspaceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            documents = documents.Where(document =>
                document.Title.Contains(term) || document.Summary.Contains(term));
        }

        var total = await documents.CountAsync(cancellationToken);
        var items = await documents
            .OrderBy(document => document.Title)
            .ThenBy(document => document.Id)
            .Skip(skip)
            .Take(limit)
            .Select(document => new DocumentSummaryResponse(
                document.Id,
                document.WorkspaceId,
                document.Workspace!.Name,
                document.Title,
                document.Summary,
                document.CoverImageUrl,
                document.Status,
                document.UpdatedAt))
            .ToListAsync(cancellationToken);

        var nextSkip = skip + items.Count;
        var nextLink = nextSkip < total
            ? Url.ActionLink(nameof(GetAll), values: new { skip = nextSkip, limit, workspaceId, query })
            : null;

        return Ok(new PagedResponse<DocumentSummaryResponse>(items, skip, limit, total, nextLink));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DocumentDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailsResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<DocumentDetailsResponse>(CacheKey(id), out var cached) && cached is not null)
        {
            Response.Headers["X-Cache"] = "HIT";
            return Ok(cached);
        }

        var document = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Workspace)
            .Include(item => item.Blocks)
            .Include(item => item.Attachments)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        var response = ToDetailsResponse(document);
        cache.Set(CacheKey(id), response, TimeSpan.FromMinutes(2));
        Response.Headers["X-Cache"] = "MISS";
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<DocumentDetailsResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentDetailsResponse>> Create(
        CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditWorkspaceAsync(request.WorkspaceId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.WorkspaceId), "The selected workspace does not exist.");
            return ValidationProblem(ModelState);
        }

        var normalizedTitle = request.Title.Trim();
        if (await dbContext.Documents.AnyAsync(
                item => item.WorkspaceId == request.WorkspaceId && item.Title == normalizedTitle,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "A document with this title already exists in the workspace.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var document = new Document
        {
            WorkspaceId = request.WorkspaceId,
            Title = normalizedTitle,
            Summary = request.Summary.Trim(),
            CoverImageUrl = NormalizeOptional(request.CoverImageUrl),
            Status = request.Status
        };

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(document).Reference(item => item.Workspace).LoadAsync(cancellationToken);

        telemetry.TrackEvent("DocumentCreated", new Dictionary<string, string>
        {
            ["DocumentId"] = document.Id.ToString(),
            ["WorkspaceId"] = document.WorkspaceId.ToString(),
            ["Status"] = document.Status.ToString()
        });
        await activityQueue.PublishAsync(new DocumentActivity(
            "document.created", document.Id, document.WorkspaceId, document.Title, DateTimeOffset.UtcNow), cancellationToken);
        if (document.Status == DocumentStatus.Published)
        {
            await notifications.DocumentPublishedAsync(document.Id, document.Title, cancellationToken);
        }

        var response = ToDetailsResponse(document);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<DocumentDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentDetailsResponse>> Update(
        Guid id,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var document = await tenantAccess.AccessibleDocuments
            .Include(item => item.Workspace)
            .Include(item => item.Blocks)
            .Include(item => item.Attachments)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (document is null)
        {
            return NotFound();
        }

        var targetWorkspace = await tenantAccess.AccessibleWorkspaces
            .SingleOrDefaultAsync(item => item.Id == request.WorkspaceId, cancellationToken);
        if (targetWorkspace is null)
        {
            ModelState.AddModelError(nameof(request.WorkspaceId), "The selected workspace does not exist.");
            return ValidationProblem(ModelState);
        }

        var normalizedTitle = request.Title.Trim();
        if (await dbContext.Documents.AnyAsync(
                item => item.Id != id && item.WorkspaceId == request.WorkspaceId && item.Title == normalizedTitle,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "A document with this title already exists in the workspace.",
                Status = StatusCodes.Status409Conflict
            });
        }

        document.WorkspaceId = request.WorkspaceId;
        document.Workspace = targetWorkspace;
        document.Title = normalizedTitle;
        document.Summary = request.Summary.Trim();
        document.CoverImageUrl = NormalizeOptional(request.CoverImageUrl);
        document.Status = request.Status;
        document.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey(id));

        telemetry.TrackEvent("DocumentUpdated", new Dictionary<string, string>
        {
            ["DocumentId"] = document.Id.ToString(),
            ["Status"] = document.Status.ToString()
        });
        await activityQueue.PublishAsync(new DocumentActivity(
            "document.updated", document.Id, document.WorkspaceId, document.Title, DateTimeOffset.UtcNow), cancellationToken);
        if (document.Status == DocumentStatus.Published)
        {
            await notifications.DocumentPublishedAsync(document.Id, document.Title, cancellationToken);
        }

        return Ok(ToDetailsResponse(document));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var document = await tenantAccess.AccessibleDocuments
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey(id));
        return NoContent();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string CacheKey(Guid id) => $"document:{currentUser.Subject}:{id:N}";

    private static DocumentDetailsResponse ToDetailsResponse(Document document) => new(
        document.Id,
        document.WorkspaceId,
        document.Workspace?.Name ?? string.Empty,
        document.Title,
        document.Summary,
        document.CoverImageUrl,
        document.Status,
        document.CreatedAt,
        document.UpdatedAt,
        document.Blocks
            .OrderBy(block => block.Position)
            .Select(block => new DocumentBlockResponse(block.Id, block.Type, block.Content, block.Position))
            .ToList(),
        document.Attachments
            .Select(attachment => new AttachmentResponse(
                attachment.Id,
                attachment.FileName,
                attachment.StorageUrl,
                attachment.ContentType,
                attachment.SizeBytes))
            .ToList());
}

