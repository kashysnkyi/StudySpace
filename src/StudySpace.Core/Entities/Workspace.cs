using System.ComponentModel.DataAnnotations;

namespace StudySpace.Core.Entities;

public sealed class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(80, MinimumLength = 3), Display(Name = "Назва")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500), Display(Name = "Опис")]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Тариф")]
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;

    [Range(-90, 90), Display(Name = "Широта")]
    public double? Latitude { get; set; }

    [Range(-180, 180), Display(Name = "Довгота")]
    public double? Longitude { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
}

public enum SubscriptionPlan
{
    [Display(Name = "Безкоштовний")]
    Free,
    [Display(Name = "Преміум")]
    Premium
}

