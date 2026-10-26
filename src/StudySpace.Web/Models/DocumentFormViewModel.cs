using System.ComponentModel.DataAnnotations;
using StudySpace.Core.Entities;

namespace StudySpace.Web.Models;

public sealed class DocumentFormViewModel
{
    public Guid? Id { get; set; }

    [Required, Display(Name = "Робочий простір")]
    public Guid WorkspaceId { get; set; }

    [Required, StringLength(120, MinimumLength = 3)]
    [Display(Name = "Назва документа")]
    public string Title { get; set; } = string.Empty;

    [StringLength(600), Display(Name = "Короткий опис")]
    public string Summary { get; set; } = string.Empty;

    [Url, StringLength(500)]
    [Display(Name = "Посилання на обкладинку")]
    public string? CoverImageUrl { get; set; }

    [Display(Name = "Статус")]
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
}

