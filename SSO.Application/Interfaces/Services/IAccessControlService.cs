using SSO.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface IAccessControlService
    {
        /// <summary>Validates that the user is allowed to access the given client application.</summary>
        Task<string?> ValidateUserAccessAsync(ApplicationUser user, string clientId);

        /// <summary>Returns all permission codes for the user scoped to a specific client application.</summary>
        Task<List<string>> GetPermissionsForUserAsync(Guid userId, Guid clientId);

        /// <summary>Gets the internal Guid of the SSO Admin client application.</summary>
        Task<Guid?> GetSSOAdminClientIdAsync();

        /// <summary>
        /// Returns the permission codes assigned to the user specifically for the SSO Admin application.
        /// Used to populate claims when a user logs in to the SSO Admin UI via cookie auth.
        /// </summary>
        Task<List<string>> GetSSOAdminPermissionsForUserAsync(Guid userId);

        /// <summary>
        /// Checks whether a master-tenant user has at least one SSO Admin role permission assigned.
        /// Returns true if the user is fully allowed; false means no SSO Admin role was assigned (deny access).
        /// </summary>
        Task<bool> IsUserSSOAdminAuthorizedAsync(ApplicationUser user);
        
        /// <summary>Fetch tenant details by client ID, and optionally pinpoint a specific mapped tenant if multiple exist.</summary>
        Task<Tenants?> GetTenantByClientIdAsync(string clientId, Guid? tenantId = null);
    }
}
