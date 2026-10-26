using System.Security.Claims;

namespace StudySpace.Web.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string Subject =>
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "demo-user";

    public string DisplayName =>
        Principal?.Identity?.Name ?? "Demo Student";
}

