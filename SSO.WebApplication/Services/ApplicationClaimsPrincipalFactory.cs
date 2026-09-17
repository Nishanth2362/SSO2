using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;
using SSO.Domain.Entities;
using System.Security.Claims;

namespace SSO.WebApplication.Services
{
    /// <summary>
    /// Custom claims principal factory that loads Permission claims from the
    /// RolePermissions table, scoped specifically to the SSO Admin client application.
    /// This ensures that when users log into the SSO Admin UI (cookie auth), they only
    /// receive permissions that have been explicitly assigned to them for this application —
    /// sub-admins with lesser permissions will only see what they're allowed to access.
    /// </summary>
    public class ApplicationClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
    {
        private readonly IAccessControlService _accessControlService;

        public ApplicationClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<IdentityOptions> options,
            IAccessControlService accessControlService)
            : base(userManager, roleManager, options)
        {
            _accessControlService = accessControlService;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // Build the base identity (includes role names, user claims, etc.)
            var identity = await base.GenerateClaimsAsync(user);

            // Load only the permissions that are scoped to the SSO Admin application.
            // This correctly handles:
            //   • Super-admins: have all permissions → see everything
            //   • Sub-admins:   have a limited role  → see only the allowed pages/APIs
            //   • Users with no SSO Admin role: IsUserSSOAdminAuthorizedAsync returns false at login
            var permissionCodes = await _accessControlService.GetSSOAdminPermissionsForUserAsync(user.Id);

            foreach (var code in permissionCodes)
            {
                if (!identity.HasClaim(ApplicationClaimTypes.Permission, code))
                {
                    identity.AddClaim(new Claim(ApplicationClaimTypes.Permission, code));
                }
            }

            return identity;
        }
    }
}
