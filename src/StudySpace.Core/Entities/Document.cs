using System.ComponentModel.DataAnnotations;

namespace StudySpace.Core.Entities;

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }

    [Required, StringLength(120, MinimumLength = 3), Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [StringLength(600), Display(Name = "Короткий опис")]
    public string Summary { get; set; } = string.Empty;

    [Url, StringLength(500), Display(Name = "Посилання на обкладинку")]
    public string? CoverImageUrl { get; set; }

    [Display(Name = "Статус")]
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentBlock> Blocks { get; set; } = new List<DocumentBlock>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}

public enum DocumentStatus
{
    [Display(Name = "Чернетка")]
    Draft,
    [Display(Name = "Опубліковано")]
    Published,
    [Display(Name = "Архів")]
    Archived
}

