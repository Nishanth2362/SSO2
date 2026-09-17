using System;
using System.Collections.Generic;

namespace SSO.Application.Configuaration
{
    public class RpapSettings
    {
        // TTL Configuration
        public int DefaultProfileTtlSeconds { get; set; } = 300;       // 5 minutes
        public int DefaultManagementTtlSeconds { get; set; } = 900;    // 15 minutes
        public int MinTtlSeconds { get; set; } = 60;                  // 1 minute
        public int MaxTtlSeconds { get; set; } = 86400;               // 24 hours

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
        public List<string> AllowedScopes { get; set; } = new()
        {
            "profile:manage",
            "users:manage",
            "users_roles:manage"
        };

        public List<string> ProfileScopeAllowedActions { get; set; } = new()
        {
            "Profile",
            "UpdateProfile",
            "ChangePassword",
            "SendEmailChangeVerification",
            "ConfirmEmailChange",
            "Enable2fa",
            "Verify2fa",
            "Disable2fa"
        };

        public List<string> UsersScopeAllowedActions { get; set; } = new()
        {
            "Users",
            "GetUsers",
            "CreateUser",
            "DeleteUser",
            "ToggleUserStatus",
            "UnlockUser",
            "ResetPasswordAdmin",
            "ExportUsers",
            "Roles",
            "GetRoles",
            "CreateRole",
            "DeleteRole",
            "RolePermissions",
            "UpdateRolePermissions",
            "GetRolesByTenant",
            "GetClientsByTenant",
            "Profile",
            "UpdateProfile",
            "ChangePassword"
        };
    }
}
