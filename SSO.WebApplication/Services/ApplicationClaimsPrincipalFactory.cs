using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public ApplicationClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<IdentityOptions> options,
            IAccessControlService accessControlService,
            ApplicationDbContext dbContext,
            IConfiguration configuration)
            : base(userManager, roleManager, options)
        {
            _accessControlService = accessControlService;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // Build the base identity (includes role names, user claims, etc.)
            var identity = await base.GenerateClaimsAsync(user);

            // Add TenantId claim
            identity.AddClaim(new Claim("TenantId", user.TenantId.ToString()));

            // Determine if the user is in the Master Tenant (SSO Admin/SuperAdmin)
            var tenantCode = await _dbContext.Tenants
                .Where(t => t.Id == user.TenantId)
                .Select(t => t.Code)
                .FirstOrDefaultAsync();

            var defaultTenantCode = _configuration.GetValue<string>("DefaultSetting:TenantCode");
            bool isMasterTenant = string.Equals(tenantCode, defaultTenantCode, StringComparison.OrdinalIgnoreCase);

            identity.AddClaim(new Claim("IsMasterTenant", isMasterTenant.ToString()));

            return identity;
        }
    }
}
