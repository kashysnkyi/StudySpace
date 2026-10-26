using Microsoft.EntityFrameworkCore;
using StudySpace.Core.Entities;
using StudySpace.Data;

namespace StudySpace.Web.Services;

public sealed class TenantAccessService(
    StudySpaceDbContext dbContext,
    CurrentUserService currentUser)
{
    public IQueryable<Workspace> AccessibleWorkspaces => dbContext.Workspaces
        .Where(workspace => workspace.Members.Any(member => member.Subject == currentUser.Subject));

    public IQueryable<Document> AccessibleDocuments => dbContext.Documents
        .Where(document => document.Workspace!.Members.Any(member => member.Subject == currentUser.Subject));

    public Task<bool> CanAccessWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        AccessibleWorkspaces.AnyAsync(workspace => workspace.Id == workspaceId, cancellationToken);

    public Task<bool> CanEditWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        dbContext.WorkspaceMembers.AnyAsync(
            member => member.WorkspaceId == workspaceId &&
                      member.Subject == currentUser.Subject &&
                      member.Role >= WorkspaceRole.Editor,
            cancellationToken);

    public Task<bool> CanAccessDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        AccessibleDocuments.AnyAsync(document => document.Id == documentId, cancellationToken);

    public Task<bool> CanEditDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        dbContext.Documents.AnyAsync(
            document => document.Id == documentId &&
                        document.Workspace!.Members.Any(member =>
                            member.Subject == currentUser.Subject && member.Role >= WorkspaceRole.Editor),
            cancellationToken);
}

