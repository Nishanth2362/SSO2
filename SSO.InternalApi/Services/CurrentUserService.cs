using SSO.Application.Interfaces.Services;
using System.Security.Claims;

namespace SSO.InternalApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId => _httpContextAccessor.HttpContext?.User == null ? Guid.Empty : GetUserId(_httpContextAccessor.HttpContext.User);

    public string UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

    public Guid TenantId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId") is string t && Guid.TryParse(t, out var id) ? id : Guid.Empty;

    public bool IsMasterTenant => _httpContextAccessor.HttpContext?.User?.FindFirstValue("IsMasterTenant") is string m && bool.TryParse(m, out var isMaster) && isMaster;

    private static Guid GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub")?.Value;
        return userIdClaim != null && Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
