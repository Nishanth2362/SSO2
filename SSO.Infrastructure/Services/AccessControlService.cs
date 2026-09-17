using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using SSO.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSO.Infrastructure.Services
{
    public class AccessControlService : IAccessControlService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly DefaultSetting _defaultSettings;
        private readonly IDapperRepository _dapper;
        private readonly IUnitOfWork<Guid> _unitOfWork;

        public AccessControlService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager,
            IDapperRepository dapper, IUnitOfWork<Guid> unitOfWork,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _roleManager = roleManager;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _dapper = dapper;
            _unitOfWork = unitOfWork;
            _defaultSettings = configuration.GetSection("DefaultSetting").Get<DefaultSetting>()!;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Core validation – called during every OpenIddict authorize/token exchange
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<string?> ValidateUserAccessAsync(ApplicationUser user, string clientId)
        {
            var client = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;
            if (client == null)
                return "The application is invalid.";

            if (!client.IsActive)
                return "The application is currently inactive.";

            // User status check
            if (!user.IsActive)
                return "Your user account is currently inactive.";

            // Tenant check
            var tenant = await _dbContext.Tenants.FindAsync(user.TenantId);
            if (tenant == null || !tenant.IsActive)
                return "Your tenant is inactive or does not exist.";

            bool isMasterTenant = (tenant.Code == _defaultSettings.TenantCode);
            bool isSsoAdminApp  = (clientId == _defaultSettings.ClientId);

            // SSO Admin UI check
            if (isSsoAdminApp && !isMasterTenant)
            {
                // We used to block all non-master tenants here. 
                // Now we allow them to fall through to line 80, where IsUserSSOAdminAuthorizedAsync
                // will check if they have a specific SSO Admin role assigned.
                // This enables sub-admins from any tenant.
            }

            // Client-Tenant restriction check
            var hasTenantClients = await _dbContext.TenantClients.AnyAsync(tc => tc.ApplicationClientId == client.Id);
            if (hasTenantClients)
            {
                var isAuthorizedForUserTenant = await _dbContext.TenantClients
                    .AnyAsync(tc => tc.ApplicationClientId == client.Id && tc.TenantId == user.TenantId);
                
                if (!isAuthorizedForUserTenant)
                    return "This application is not authorized for your tenant.";
            }

            // ──────────────────────────────────────────────────────────────────────
            // SSO Admin: check that the master-tenant user has an SSO Admin role.
            // This enables "lesser permission" sub-admins: they can log in only if
            // at least one role + permission is mapped to the SSO Admin client.
            // ──────────────────────────────────────────────────────────────────────
            if (isSsoAdminApp)
            {
                if (isMasterTenant)
                {
                    var isAuthorized = await IsUserSSOAdminAuthorizedAsync(user);
                    if (!isAuthorized)
                        return "You have not been assigned any SSO Admin roles. Contact your system administrator.";
                }
                else
                {
                    // Allow client tenant users to log in if they have at least one role assigned in their tenant
                    var hasRoles = await (from ur in _dbContext.UserRoles
                                          join r in _dbContext.Roles on ur.RoleId equals r.Id
                                          where ur.UserId == user.Id && r.TenantId == user.TenantId
                                          select ur.RoleId).AnyAsync();
                    if (!hasRoles)
                        return "You have not been assigned any roles in your organization. Contact your administrator.";
                }

                // Skip the OpenIddict consent check for SSO Admin (it's a first-party app)
                return null;
            }

            // ──────────────────────────────────────────────────────────────────────
            // Non-SSO-Admin clients: explicit user → client consent check
            // ──────────────────────────────────────────────────────────────────────
            var authorizations = _authorizationManager.FindAsync(
                subject: user.Id.ToString(),
                client: client.Id.ToString(),
                status: OpenIddictConstants.Statuses.Valid,
                type: OpenIddictConstants.AuthorizationTypes.Permanent,
                scopes: null);

            bool hasConsent = false;
            await foreach (var auth in authorizations)
            {
                hasConsent = true;
                break;
            }

            if (!hasConsent)
                return "You have not been granted access to this application.";

            // ──────────────────────────────────────────────────────────────────────
            // Billing checks (skip for Master Tenant)
            // ──────────────────────────────────────────────────────────────────────
            if (!isMasterTenant)
            {
                var activeSubscription = await _dbContext.TenantSubscriptions
                    .FirstOrDefaultAsync(s => s.TenantId == user.TenantId
                                && s.IsActive
                                && s.StartDateUtc <= DateTime.UtcNow
                                && s.EndDateUtc    >= DateTime.UtcNow);

                if (activeSubscription == null)
                    return "Your tenant does not have an active subscription.";

                // Check for unpaid invoices on the active subscription with grace period
                var blockDate = DateTime.UtcNow;
                var hasUnpaidInvoicesPastGrace = await _dbContext.Invoices
                    .AnyAsync(i => i.TenantSubscriptionId == activeSubscription.Id
                                && (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue)
                                && i.DueDate.AddDays(tenant.GracePeriodDays) < blockDate);

                if (hasUnpaidInvoicesPastGrace)
                    return $"Your tenant has unpaid invoices that are past the grace period ({tenant.GracePeriodDays} days). Please settle outstanding payments to continue using this service.";
            }

            // ──────────────────────────────────────────────────────────────────────
            // Client-specific role permission check
            // ──────────────────────────────────────────────────────────────────────
            var roles = await (from r in _dbContext.Roles
                               join ur in _dbContext.UserRoles on r.Id equals ur.RoleId
                               where ur.UserId == user.Id
                               select r.Id).ToListAsync();

            bool specificRoleRequired = await _dbContext.RolePermissions
                .AnyAsync(rp => rp.ApplicationClientId == client.Id);

            if (specificRoleRequired)
            {
                if (!roles.Any())
                    return "You do not have the required role permissions for this application.";

                var hasClientRole = await (from ur in _dbContext.UserRoles
                                           join rp in _dbContext.RolePermissions on ur.RoleId equals rp.RoleId
                                           where ur.UserId == user.Id && rp.ApplicationClientId == client.Id
                                           select ur.RoleId).AnyAsync();

                if (!hasClientRole)
                    return "You do not have the required role permissions for this application.";
            }

            return null; // All checks passed
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Returns permission codes for a user scoped to a specific client
        // (used when signing in via password / authorization-code flow)
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<List<string>> GetPermissionsForUserAsync(Guid userId, Guid clientId)
        {
            // 1. Explicit permissions: Role is directly mapped to this ClientId
            var explicitPermissions = await (from rp in _dbContext.RolePermissions
                                             join ur in _dbContext.UserRoles on rp.RoleId equals ur.RoleId
                                             where ur.UserId == userId && rp.ApplicationClientId == clientId
                                             select rp.Permission.Code)
                                             .ToListAsync();

            // 2. Scope-based permissions (Combined logic):
            // Get all permissions the user has across ALL their roles and intersect them 
            // with the permissions allowed by the current client's scopes.
            var allUserPermissionsAcrossRoles = await (from rp in _dbContext.RolePermissions
                                                       join ur in _dbContext.UserRoles on rp.RoleId equals ur.RoleId
                                                       where ur.UserId == userId
                                                       select rp.Permission.Code)
                                                       .Distinct()
                                                       .ToListAsync();

            var clientAllowedPermissionsByScope = await (from cs in _dbContext.ApplicationClientScopes
                                                        join sp in _dbContext.ApplicationScopePermissions on cs.ScopeId equals sp.ScopeId
                                                        where cs.ClientId == clientId
                                                        select sp.Permission.Code)
                                                        .Distinct()
                                                        .ToListAsync();

            var scopeCombinedPermissions = allUserPermissionsAcrossRoles
                .Intersect(clientAllowedPermissionsByScope)
                .ToList();

            // Return the union of explicit and scope-combined permissions
            return explicitPermissions
                .Union(scopeCombinedPermissions)
                .Distinct()
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Resolves the internal DB Guid for the SSO Admin ApplicationClient
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<Guid?> GetSSOAdminClientIdAsync()
        {
            var adminApp = await _dbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClientId == _defaultSettings.ClientId);

            return adminApp?.Id;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Permissions for the SSO Admin portal (cookie-based login)
        // These are loaded by ApplicationClaimsPrincipalFactory at sign-in
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<List<string>> GetSSOAdminPermissionsForUserAsync(Guid userId)
        {
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return new List<string>();

            var tenant = await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == user.TenantId);
            bool isMasterTenant = (tenant?.Code == _defaultSettings.TenantCode);

            if (isMasterTenant)
            {
                var adminClientId = await GetSSOAdminClientIdAsync();
                if (adminClientId == null)
                    return new List<string>();

                return await GetPermissionsForUserAsync(userId, adminClientId.Value);
            }
            else
            {
                // Non-master tenant user logging into the SSO portal (restricted view context):
                // Load all permissions mapped to their roles across all client applications.
                var permissions = await (from rp in _dbContext.RolePermissions
                                         join ur in _dbContext.UserRoles on rp.RoleId equals ur.RoleId
                                         where ur.UserId == userId
                                         select rp.Permission.Code)
                                         .Distinct()
                                         .ToListAsync();
                return permissions;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Returns true when the master-tenant user has at least ONE role permission
        // assigned specifically for the SSO Admin application client.
        // Super-admins (with ALL permissions seeded) will always pass.
        // Sub-admins (limited role) pass only if their role is mapped.
        // Ordinary master-tenant users with no SSO Admin role → denied.
        // ─────────────────────────────────────────────────────────────────────────
        public async Task<bool> IsUserSSOAdminAuthorizedAsync(ApplicationUser user)
        {
            var adminClientId = await GetSSOAdminClientIdAsync();
            if (adminClientId == null)
            {
                // If the SSO Admin app doesn't exist in DB yet, fall back to allowing master-tenant users
                return true;
            }

            // Check whether the user has any role that has been granted permissions for the SSO Admin client
            var hasSSOAdminRole = await (from ur in _dbContext.UserRoles
                                         join rp in _dbContext.RolePermissions on ur.RoleId equals rp.RoleId
                                         where ur.UserId == user.Id && rp.ApplicationClientId == adminClientId.Value
                                         select ur.RoleId).AnyAsync();

            return hasSSOAdminRole;
        }

        public async Task<Tenants?> GetTenantByClientIdAsync(string clientId, Guid? tenantId = null)
        {
            var client = await _applicationManager.FindByClientIdAsync(clientId) as ApplicationClient;
            if (client == null) return null;

            var hasTenantClients = await _dbContext.TenantClients
                .AnyAsync(tc => tc.ApplicationClientId == client.Id);

            if (!hasTenantClients)
            {
                // Fallback to Master Tenant if no specific tenant is assigned to the client
                return await _dbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Code == _defaultSettings.TenantCode);
            }

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                var specificTenant = await _dbContext.TenantClients
                    .Where(tc => tc.ApplicationClientId == client.Id && tc.TenantId == tenantId.Value)
                    .Select(tc => tc.Tenant)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return specificTenant;
            }

            var firstMappedTenant = await _dbContext.TenantClients
                .Where(tc => tc.ApplicationClientId == client.Id)
                .OrderBy(tc => tc.Tenant.Code)
                .Select(tc => tc.Tenant)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return firstMappedTenant;
        }
    }
}
