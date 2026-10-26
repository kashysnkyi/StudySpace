using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;
using StudySpace.Data;
using StudySpace.Web.Models;
using StudySpace.Web.Services;

namespace StudySpace.Web.Controllers;

public sealed class DocumentsController(
    StudySpaceDbContext dbContext,
    TenantAccessService tenantAccess,
    IAttachmentStorage attachmentStorage) : Controller
{
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var document = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .Include(item => item.Workspace)
            .Include(item => item.Blocks.OrderBy(block => block.Position))
            .Include(item => item.Attachments)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return document is null ? NotFound() : View(document);
    }

    public async Task<IActionResult> EditContent(Guid id, CancellationToken cancellationToken)
    {
        var document = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .Include(item => item.Workspace)
            .Include(item => item.Blocks.OrderBy(block => block.Position))
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return document is null ? NotFound() : View(document);
    }

    public async Task<IActionResult> Create(Guid? workspaceId, CancellationToken cancellationToken)
    {
        await PopulateWorkspacesAsync(workspaceId, cancellationToken);
        return View(new DocumentFormViewModel { WorkspaceId = workspaceId ?? Guid.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DocumentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditWorkspaceAsync(model.WorkspaceId, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.WorkspaceId), "Select an existing workspace.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateWorkspacesAsync(model.WorkspaceId, cancellationToken);
            return View(model);
        }

        var document = new Document
        {
            WorkspaceId = model.WorkspaceId,
            Title = model.Title.Trim(),
            Summary = model.Summary.Trim(),
            CoverImageUrl = string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl.Trim(),
            Status = model.Status
        };

        dbContext.Documents.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = $"Документ «{document.Title}» створено.";
        return RedirectToAction(nameof(Details), new { document.Id });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var document = await tenantAccess.AccessibleDocuments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        if (!await tenantAccess.CanEditDocumentAsync(id, cancellationToken))
        {
            return Forbid();
        }

        await PopulateWorkspacesAsync(document.WorkspaceId, cancellationToken);
        return View(new DocumentFormViewModel
        {
            Id = document.Id,
            WorkspaceId = document.WorkspaceId,
            Title = document.Title,
            Summary = document.Summary,
            CoverImageUrl = document.CoverImageUrl,
            Status = document.Status
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DocumentFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateWorkspacesAsync(model.WorkspaceId, cancellationToken);
            return View(model);
        }

        if (!await tenantAccess.CanEditDocumentAsync(id, cancellationToken) ||
            !await tenantAccess.CanEditWorkspaceAsync(model.WorkspaceId, cancellationToken))
        {
            return Forbid();
        }

        var document = await tenantAccess.AccessibleDocuments
            .Include(item => item.Workspace)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        document.WorkspaceId = model.WorkspaceId;
        document.Title = model.Title.Trim();
        document.Summary = model.Summary.Trim();
        document.CoverImageUrl = string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl.Trim();
        document.Status = model.Status;
        document.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Документ оновлено.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var document = await tenantAccess.AccessibleDocuments
            .AsNoTracking()
            .Include(item => item.Workspace)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return document is null ? NotFound() : View(document);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var document = await tenantAccess.AccessibleDocuments
            .Include(item => item.Workspace)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var workspaceId = document.WorkspaceId;
        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Документ видалено.";
        return RedirectToAction("Details", "Workspaces", new { id = workspaceId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        Guid id,
        IFormFile? file,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var document = await tenantAccess.AccessibleDocuments
            .Include(item => item.Workspace)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var maximumSize = document.Workspace?.Plan == SubscriptionPlan.Premium
            ? 10 * 1024 * 1024
            : 2 * 1024 * 1024;
        if (file is null || file.Length is <= 0)
        {
            TempData["Error"] = "Оберіть файл перед завантаженням.";
            return RedirectAfterFileAction(id, returnUrl);
        }

        if (file.Length > maximumSize)
        {
            TempData["Error"] = $"Оберіть файл розміром до {maximumSize / 1024 / 1024} МБ для цього тарифу.";
            return RedirectAfterFileAction(id, returnUrl);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var permittedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".txt", ".md"
        };

        if (!permittedExtensions.Contains(extension))
        {
            TempData["Error"] = "Цей тип файлу не підтримується.";
            return RedirectAfterFileAction(id, returnUrl);
        }

        var storedName = $"{Guid.NewGuid():N}{extension}";
        await using var source = file.OpenReadStream();
        var storageUrl = await attachmentStorage.SaveAsync(storedName, source, file.ContentType, cancellationToken);

        dbContext.Attachments.Add(new Attachment
        {
            DocumentId = id,
            FileName = Path.GetFileName(file.FileName),
            StorageUrl = storageUrl,
            ContentType = file.ContentType,
            SizeBytes = file.Length
        });
        document.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = "Файл прикріплено до документа.";
        return RedirectAfterFileAction(id, returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(
        Guid id,
        Guid attachmentId,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        if (!await tenantAccess.CanEditDocumentAsync(id, cancellationToken))
        {
            return Forbid();
        }

        var attachment = await dbContext.Attachments
            .SingleOrDefaultAsync(item => item.Id == attachmentId && item.DocumentId == id, cancellationToken);
        if (attachment is null)
        {
            return NotFound();
        }

        await attachmentStorage.DeleteAsync(attachment.StorageUrl, cancellationToken);
        dbContext.Attachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["Message"] = $"Файл «{attachment.FileName}» видалено.";
        return RedirectAfterFileAction(id, returnUrl);
    }

    private async Task PopulateWorkspacesAsync(Guid? selectedId, CancellationToken cancellationToken)
    {
        var workspaces = await tenantAccess.AccessibleWorkspaces.AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        ViewBag.Workspaces = new SelectList(workspaces, nameof(Workspace.Id), nameof(Workspace.Name), selectedId);
    }

    private IActionResult RedirectAfterFileAction(Guid documentId, string? returnUrl) =>
        Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Details), new { id = documentId });
}

