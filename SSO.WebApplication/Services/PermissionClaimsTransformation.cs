using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;

namespace SSO.WebApplication.Services
{
    public class PermissionClaimsTransformation : IClaimsTransformation
    {
        private readonly IAccessControlService _accessControlService;

        public PermissionClaimsTransformation(IAccessControlService accessControlService)
        {
            _accessControlService = accessControlService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Clone principal to avoid mutating the cached cookie principal directly
            var clone = principal.Clone();
            var identity = (ClaimsIdentity?)clone.Identity;

            if (identity == null || !identity.IsAuthenticated)
            {
                return principal;
            }

            var userIdClaim = clone.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return principal;
            }

            // Load permissions on-demand for this request
            var permissionCodes = await _accessControlService.GetSSOAdminPermissionsForUserAsync(userId);

            foreach (var code in permissionCodes)
            {
                if (!identity.HasClaim(ApplicationClaimTypes.Permission, code))
                {
                    identity.AddClaim(new Claim(ApplicationClaimTypes.Permission, code));
                }
            }

            return clone;
        }
    }
}
