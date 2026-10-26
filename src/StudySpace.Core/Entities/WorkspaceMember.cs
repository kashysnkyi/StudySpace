using System.ComponentModel.DataAnnotations;

namespace StudySpace.Core.Entities;

public sealed class WorkspaceMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    [Required, StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    public WorkspaceRole Role { get; set; } = WorkspaceRole.Viewer;
}

public enum WorkspaceRole
{
    Viewer,
    Editor,
    Owner
}

