using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;
using StudySpace.Data;
using StudySpace.Web.Contracts.Blocks;
using StudySpace.Web.Hubs;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers.Api;

[ApiController]
[Route("api/documents/{documentId:guid}/blocks")]
public sealed class BlocksApiController(
    StudySpaceDbContext dbContext,
    IHubContext<CollaborationHub> hubContext,
    TenantAccessService tenantAccess) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlockResponse>>> GetAll(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanAccessDocumentAsync(documentId, cancellationToken))
        {
            return NotFound();
        }

        var blocks = await dbContext.DocumentBlocks
            .AsNoTracking()
            .Where(block => block.DocumentId == documentId)
            .OrderBy(block => block.Position)
            .Select(block => new BlockResponse(block.Id, block.Type, block.Content, block.Position))
            .ToListAsync(cancellationToken);

        return Ok(blocks);
    }

    [HttpPost]
    public async Task<ActionResult<BlockResponse>> Create(
        Guid documentId,
        CreateBlockRequest request,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(documentId, cancellationToken))
        {
            return Forbid();
        }

        var document = await tenantAccess.AccessibleDocuments
            .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var nextPosition = await dbContext.DocumentBlocks
            .Where(block => block.DocumentId == documentId)
            .Select(block => (int?)block.Position)
            .MaxAsync(cancellationToken) + 1 ?? 0;

        var block = new DocumentBlock
        {
            DocumentId = documentId,
            Type = request.Type,
            Content = request.Content.Trim(),
            Position = nextPosition
        };

        dbContext.DocumentBlocks.Add(block);
        document.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(block);
        await BroadcastAsync(documentId, "blockCreated", response, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { documentId }, response);
    }

    [HttpPut("{blockId:guid}")]
    public async Task<ActionResult<BlockResponse>> Update(
        Guid documentId,
        Guid blockId,
        UpdateBlockRequest request,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(documentId, cancellationToken))
        {
            return Forbid();
        }

        var block = await dbContext.DocumentBlocks
            .SingleOrDefaultAsync(item => item.Id == blockId && item.DocumentId == documentId, cancellationToken);
        if (block is null)
        {
            return NotFound();
        }

        block.Type = request.Type;
        block.Content = request.Content.Trim();
        await TouchDocumentAsync(documentId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(block);
        await BroadcastAsync(documentId, "blockUpdated", response, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{blockId:guid}")]
    public async Task<IActionResult> Delete(Guid documentId, Guid blockId, CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(documentId, cancellationToken))
        {
            return Forbid();
        }

        var block = await dbContext.DocumentBlocks
            .SingleOrDefaultAsync(item => item.Id == blockId && item.DocumentId == documentId, cancellationToken);
        if (block is null)
        {
            return NotFound();
        }

        dbContext.DocumentBlocks.Remove(block);
        await TouchDocumentAsync(documentId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await BroadcastAsync(documentId, "blockDeleted", new { blockId }, cancellationToken);
        return NoContent();
    }

    [HttpPut("order")]
    public async Task<IActionResult> Reorder(
        Guid documentId,
        ReorderBlocksRequest request,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(documentId, cancellationToken))
        {
            return Forbid();
        }

        var blocks = await dbContext.DocumentBlocks
            .Where(block => block.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        if (blocks.Count != request.BlockIds.Count ||
            blocks.Select(block => block.Id).ToHashSet().SetEquals(request.BlockIds) is false)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "The order must contain every document block exactly once.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var positionById = request.BlockIds
            .Select((id, position) => new { id, position })
            .ToDictionary(item => item.id, item => item.position);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        for (var index = 0; index < blocks.Count; index++)
        {
            blocks[index].Position = -(index + 1);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var block in blocks)
        {
            block.Position = positionById[block.Id];
        }

        await TouchDocumentAsync(documentId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await BroadcastAsync(documentId, "blocksReordered", request.BlockIds, cancellationToken);
        return NoContent();
    }

    private async Task TouchDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.Documents.FindAsync([documentId], cancellationToken);
        if (document is not null)
        {
            document.UpdatedAt = DateTime.UtcNow;
        }
    }

    private Task BroadcastAsync(Guid documentId, string eventName, object payload, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(CollaborationHub.GroupName(documentId))
            .SendAsync(eventName, payload, cancellationToken);

    private static BlockResponse ToResponse(DocumentBlock block) =>
        new(block.Id, block.Type, block.Content, block.Position);
}

