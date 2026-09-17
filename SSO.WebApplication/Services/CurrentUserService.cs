using SSO.Application.Interfaces.Services;
using System.Security.Claims;

namespace SSO.WebApplication.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId => _httpContextAccessor.HttpContext?.User == null ? Guid.Empty : GetUserId(_httpContextAccessor.HttpContext.User);

        public List<KeyValuePair<string, string>> Claims => 
            _httpContextAccessor.HttpContext?.User?.Claims
                .Select(item => new KeyValuePair<string, string>(item.Type, item.Value))
                .ToList() ?? new List<KeyValuePair<string, string>>();

        public string UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

        public string IpAddress => _httpContextAccessor.HttpContext?.Request?.Host.Value;

        public string GateNo => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Surname);

        public bool IsAdmin => _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;

        public Guid TenantId => _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId") is string t && Guid.TryParse(t, out var id) ? id : Guid.Empty;

        public bool IsMasterTenant => _httpContextAccessor.HttpContext?.User?.FindFirstValue("IsMasterTenant") is string m && bool.TryParse(m, out var isMaster) && isMaster;

        private Guid GetUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirst("sub")?.Value;
            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }
    }
}
