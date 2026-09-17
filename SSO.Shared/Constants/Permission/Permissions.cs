using System.ComponentModel;
using System.Reflection;

namespace SSO.Common.Constants.Permission
{
    public static class Permissions
    {
        [DisplayName("Subscription")]
        [Description("Subscription Permissions")]
        public static class Subscription
        {
            public const string View = "Permissions.Subscription.View";
            public const string Create = "Permissions.Subscription.Create";
            public const string Edit = "Permissions.Subscription.Edit";
            public const string Delete = "Permissions.Subscription.Delete";
            public const string Export = "Permissions.Subscription.Export";
            public const string Search = "Permissions.Subscription.Search";
        }


        [DisplayName("Client")]
        [Description("Client Permissions")]
        public static class Client
        {
            public const string View = "Permissions.Client.View";
            public const string Create = "Permissions.Client.Create";
            public const string Edit = "Permissions.Client.Edit";
            public const string Delete = "Permissions.Client.Delete";
            public const string Export = "Permissions.Client.Export";
            public const string Search = "Permissions.Client.Search";
        }

        [DisplayName("Tenant")]
        [Description("Tenant Permissions")]
        public static class Tenant
        {
            public const string View = "Permissions.Tenant.View";
            public const string Create = "Permissions.Tenant.Create";
            public const string Edit = "Permissions.Tenant.Edit";
            public const string Delete = "Permissions.Tenant.Delete";
            public const string Export = "Permissions.Tenant.Export";
            public const string Search = "Permissions.Tenant.Search";
        }

        [DisplayName("Templates")]
        [Description("Templates Permissions")]
        public static class Templates
        {
            public const string View = "Permissions.Templates.View";
            public const string Create = "Permissions.Templates.Create";
            public const string Edit = "Permissions.Templates.Edit";
            public const string Delete = "Permissions.Templates.Delete";
            public const string Export = "Permissions.Templates.Export";
            public const string Search = "Permissions.Templates.Search";
        }

        [DisplayName("Email Templates")]
        [Description("Email Templates Permissions")]
        public static class EmailTemplates
        {
            public const string View = "Permissions.EmailTemplates.View";
            public const string Create = "Permissions.EmailTemplates.Create";
            public const string Edit = "Permissions.EmailTemplates.Edit";
            public const string Delete = "Permissions.EmailTemplates.Delete";
        }
        
       
        [DisplayName("Users")]
        [Description("Users Permissions")]
        public static class Users
        {
            public const string View = "Permissions.Users.View";
            public const string Create = "Permissions.Users.Create";
            public const string Edit = "Permissions.Users.Edit";
            public const string Delete = "Permissions.Users.Delete";
            public const string Export = "Permissions.Users.Export";
            public const string Search = "Permissions.Users.Search";
        }

        [DisplayName("Roles")]
        [Description("Roles Permissions")]
        public static class Roles
        {
            public const string View = "Permissions.Roles.View";
            public const string Create = "Permissions.Roles.Create";
            public const string Edit = "Permissions.Roles.Edit";
            public const string Delete = "Permissions.Roles.Delete";
            public const string Search = "Permissions.Roles.Search";
        }

        [DisplayName("Role Claims")]
        [Description("Role Claims Permissions")]
        public static class RoleClaims
        {
            public const string View = "Permissions.RoleClaims.View";
            public const string Create = "Permissions.RoleClaims.Create";
            public const string Edit = "Permissions.RoleClaims.Edit";
            public const string Delete = "Permissions.RoleClaims.Delete";
            public const string Search = "Permissions.RoleClaims.Search";
        }


        [DisplayName("Preferences")]
        [Description("Preferences Permissions")]
        public static class Preferences
        {
            public const string ChangeLanguage = "Permissions.Preferences.ChangeLanguage";

            //TODO - add permissions
        }

        [DisplayName("Dashboards")]
        [Description("Dashboards Permissions")]
        public static class Dashboards
        {
            public const string View = "Permissions.Dashboards.View";
        }

        [DisplayName("Hangfire")]
        [Description("Hangfire Permissions")]
        public static class Hangfire
        {
            public const string View = "Permissions.Hangfire.View";
            public const string Manage = "Permissions.Hangfire.Manage";
        }

        [DisplayName("Audit Trails")]
        [Description("Audit Trails Permissions")]
        public static class AuditTrails
        {
            public const string View = "Permissions.AuditTrails.View";
            public const string Export = "Permissions.AuditTrails.Export";
            public const string Search = "Permissions.AuditTrails.Search";
        }

        /// <summary>
        /// SSO Admin Panel – coarse-grained access levels for sub-administrators.
        /// Assign these permissions (via a limited role) to users who should be
        /// able to log into SSO Admin but with reduced access.
        /// </summary>
        [DisplayName("SSO Admin")]
        [Description("SSO Admin Panel Access Levels")]
        public static class SSOAdmin
        {
            /// <summary>Can log in to SSO Admin and view the dashboard.</summary>
            public const string Access = "Permissions.SSOAdmin.Access";

            /// <summary>Can view tenants (read-only).</summary>
            public const string ViewTenants = "Permissions.SSOAdmin.ViewTenants";

            /// <summary>Can manage (create/edit/delete) tenants.</summary>
            public const string ManageTenants = "Permissions.SSOAdmin.ManageTenants";

            /// <summary>Can view users (read-only).</summary>
            public const string ViewUsers = "Permissions.SSOAdmin.ViewUsers";

            /// <summary>Can manage (create/edit/delete) users.</summary>
            public const string ManageUsers = "Permissions.SSOAdmin.ManageUsers";

            /// <summary>Can view client applications (read-only).</summary>
            public const string ViewClients = "Permissions.SSOAdmin.ViewClients";

            /// <summary>Can manage (create/edit/delete) client applications.</summary>
            public const string ManageClients = "Permissions.SSOAdmin.ManageClients";

            /// <summary>Can view roles and their permission assignments (read-only).</summary>
            public const string ViewRoles = "Permissions.SSOAdmin.ViewRoles";

            /// <summary>Can manage roles and assign permissions.</summary>
            public const string ManageRoles = "Permissions.SSOAdmin.ManageRoles";

            /// <summary>Can view subscription plans.</summary>
            public const string ViewSubscriptions = "Permissions.SSOAdmin.ViewSubscriptions";

            /// <summary>Can manage subscription plans.</summary>
            public const string ManageSubscriptions = "Permissions.SSOAdmin.ManageSubscriptions";
        }

        [DisplayName("Scope")]
        [Description("Scope Permissions")]
        public static class Scope
        {
            public const string View = "Permissions.Scope.View";
            public const string Create = "Permissions.Scope.Create";
            public const string Edit = "Permissions.Scope.Edit";
            public const string Delete = "Permissions.Scope.Delete";
            public const string Export = "Permissions.Scope.Export";
            public const string Search = "Permissions.Scope.Search";
            public const string Import = "Permissions.Scope.Import";
        }

        /// <summary>
        /// Returns the minimal set of permissions for a read-only SSO Admin sub-administrator.
        /// Useful for seeding a default "SSO Admin Viewer" role.
        /// </summary>
        public static List<string> GetSSOAdminViewerPermissions()
        {
            return new List<string>
            {
                // Base SSO Admin views
                SSOAdmin.Access,
                SSOAdmin.ViewTenants,
                SSOAdmin.ViewUsers,
                SSOAdmin.ViewClients,
                SSOAdmin.ViewRoles,
                SSOAdmin.ViewSubscriptions,
                // Standard read-only permissions that map to the existing policy checks
                Tenant.View,
                Users.View,
                Client.View,
                Roles.View,
                RoleClaims.View,
                Subscription.View,
                Dashboards.View,
            };
        }

        /// <summary>
        /// Returns a list of Permissions.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetRegisteredPermissions()
        {
            List<string> permissions = new();
            foreach (FieldInfo? prop in typeof(Permissions).GetNestedTypes().SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
            {
                object? propertyValue = prop.GetValue(null);
                if (propertyValue is not null)
                {
                    permissions.Add(propertyValue.ToString()!);
                }
            }
            return permissions;
        }
    }
}
