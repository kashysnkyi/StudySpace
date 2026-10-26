using System.ComponentModel.DataAnnotations;

namespace StudySpace.Web.Models;

public sealed class LoginViewModel
{
    [Required, StringLength(80), Display(Name = "Ім'я")]
    public string DisplayName { get; init; } = "Демонстраційний студент";

    [Required, RegularExpression("^[a-z0-9-]{3,40}$"), Display(Name = "Ідентифікатор")]
    public string Subject { get; init; } = "demo-user";

    public bool GoogleEnabled { get; init; }
}

