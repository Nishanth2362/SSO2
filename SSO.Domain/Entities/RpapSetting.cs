using SSO.Domain.Contract;
using System;

namespace SSO.Domain.Entities
{
    /// <summary>
    /// Database entity storing configurable parameters for the Restricted Page Access Protocol (RPAP) & SsoContextFilter.
    /// </summary>
    public class RpapSetting : AuditableEntity<Guid>
    {
        // TTL Configuration
        public int DefaultProfileTtlSeconds { get; set; } = 300;
        public int DefaultManagementTtlSeconds { get; set; } = 900;
        public int MinTtlSeconds { get; set; } = 60;
        public int MaxTtlSeconds { get; set; } = 86400;

        // Routing & Landing URLs
        public string ProfileLandingUrl { get; set; } = "/Account/Profile";
        public string UsersLandingUrl { get; set; } = "/Home/Users";
        public string FallbackUnauthorizedRedirectUrl { get; set; } = "/Account/Profile";

        // Protocol Security Flags
        public bool AutoRevokeOnResultExchange { get; set; } = true;
        public bool RequireStrictLocalhostPort { get; set; } = false;
        public bool EnforceScopeAccess { get; set; } = true;
        public int ContextCookieExpiryDays { get; set; } = 7;

        // Scopes & Filter Allowed Actions
        public string AllowedScopes { get; set; } = "profile:manage,users:manage,users_roles:manage";
        public string ProfileScopeAllowedActions { get; set; } = "Profile,UpdateProfile,ChangePassword,SendEmailChangeVerification,ConfirmEmailChange,Enable2fa,Verify2fa,Disable2fa";
        public string UsersScopeAllowedActions { get; set; } = "Users,GetUsers,CreateUser,DeleteUser,ToggleUserStatus,UnlockUser,ResetPasswordAdmin,ExportUsers,Roles,GetRoles,CreateRole,DeleteRole,RolePermissions,UpdateRolePermissions,GetRolesByTenant,GetClientsByTenant,Profile,UpdateProfile,ChangePassword";

        public Guid? TenantId { get; set; } // null = Global default
    }
}
