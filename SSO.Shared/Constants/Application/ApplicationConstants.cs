using System;
using System.Collections.Generic;
using System.Linq;

namespace SSO.Common.Constants.Application
{
    public static class ApplicationConstants
    {
        public static class SignalR
        {
            public const string HubUrl = "/signalRHub";
            public const string SendUpdateDashboard = "UpdateDashboardAsync";
            public const string ReceiveUpdateDashboard = "UpdateDashboard";
            public const string SendRegenerateTokens = "RegenerateTokensAsync";
            public const string ReceiveRegenerateTokens = "RegenerateTokens";
            public const string ReceiveChatNotification = "ReceiveChatNotification";
            public const string SendChatNotification = "ChatNotificationAsync";
            public const string ReceiveMessage = "ReceiveMessage";
            public const string SendMessage = "SendMessageAsync";

            public const string OnConnect = "OnConnectAsync";
            public const string ConnectUser = "ConnectUser";
            public const string OnDisconnect = "OnDisconnectAsync";
            public const string DisconnectUser = "DisconnectUser";
            public const string OnChangeRolePermissions = "OnChangeRolePermissions";
            public const string LogoutUsersByRole = "LogoutUsersByRole";

            public const string PingRequest = "PingRequestAsync";
            public const string PingResponse = "PingResponseAsync";
        }
       
        public static class Authenticator
        {
            public const string LoginProvider = "AspNet.Identity.AuthenticatorKey";
            public const string TokenName = "AuthenticatorKey";
        }

        public static class Cache
        {
            public const string GetAllDepartmentCacheKey = "all-departments";
            public const string GetAllJobCategoryCacheKey = "all-job-categories";
            public const string GetAllJobLocationCacheKey = "all-job-loctions";
            public const string GetAllRoundCacheKey = "all-rounds";
            public const string GetAllOrganisationLocationCacheKey = "organisation-locations";
            public const string GetAllJobCacheKey = "all-jobs";
        }

        public static class HeaderType
        {
            public const string ClientIdHeader = "x-client-id";
            public const string ClientSecrectHeader = "x-client-secrect";
        }

        public static class MimeTypes
        {
            public const string OpenXml = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public const string OctetStream = "application/octet-stream";
        }

        public static class OpenIdDictCustomConstants
        {
            public const string TenantId = "tenant_id";
            public const string TenantName = "tenant_name";
            public const string TenantCode = "tenant_code";
            public const string TenantDatabaseProvider = "tenant_database_provider";
            public const string TenantDatabaseName = "tenant_database_name";
        }

        public static class DBProvider
        {
            public const string Mysql = "Mysql";
            public const string SqlServer = "SqlServer";
            public const string PostgreSql = "PostgreSql";
            public const string Oracle = "Oracle";
        }

        public static class RpapCookies
        {
            public const string TransactionId = "RPAP_TransactionId";
            public const string Scope = "RPAP_Scope";
            public const string TenantId = "RPAP_TenantId";
            public const string ClientId = "RPAP_ClientId";
            public const string CallbackUrl = "RPAP_CallbackUrl";
            public const string ClientName = "RPAP_ClientName";
            public const string SsoViewContext = "SSO_ViewContext";
            public const string SsoReturnUrl = "SSO_ReturnUrl";

            public static readonly string[] AllRpapCookies = new[]
            {
                TransactionId,
                Scope,
                TenantId,
                ClientId,
                CallbackUrl,
                ClientName,
                SsoViewContext,
                SsoReturnUrl
            };
        }

        public static class RpapConfig
        {
            public static class Scopes
            {
                public const string ProfileManage = "profile:manage";
                public const string UsersManage = "users:manage";
                public const string UsersRolesManage = "users_roles:manage";

                public static readonly string[] AllSupportedScopes = new[]
                {
                    ProfileManage,
                    UsersManage,
                    UsersRolesManage
                };

                public static bool IsValid(string? scope)
                {
                    if (string.IsNullOrWhiteSpace(scope)) return false;
                    return AllSupportedScopes.Contains(scope.Trim().ToLowerInvariant());
                }
            }

            public static class LandingUrls
            {
                public const string Profile = "/Account/Profile";
                public const string Users = "/Home/Users";

                public static readonly IReadOnlyDictionary<string, string> ScopeLandingUrlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { Scopes.ProfileManage, Profile },
                    { Scopes.UsersManage, Users },
                    { Scopes.UsersRolesManage, Users }
                };

                public static string ResolveLandingUrl(string? scope, string? requestedTargetUrl = null)
                {
                    if (!string.IsNullOrWhiteSpace(requestedTargetUrl))
                        return requestedTargetUrl;

                    if (!string.IsNullOrWhiteSpace(scope) && ScopeLandingUrlMap.TryGetValue(scope.Trim(), out var url))
                        return url;

                    return Profile;
                }
            }

            public static class Ttl
            {
                public const int MinTtlSeconds = 60;          // 1 minute minimum
                public const int MaxTtlSeconds = 86400;       // 24 hours maximum
                public const int DefaultProfileTtl = 300;     // 5 minutes for profile
                public const int DefaultManagementTtl = 900;  // 15 minutes for user management

                public static int ResolveTtl(int? requestedTtlSeconds, string? scope)
                {
                    int ttl;
                    if (requestedTtlSeconds.HasValue && requestedTtlSeconds.Value > 0)
                    {
                        ttl = requestedTtlSeconds.Value;
                    }
                    else
                    {
                        var normalized = scope?.Trim().ToLowerInvariant();
                        ttl = (normalized == Scopes.ProfileManage) ? DefaultProfileTtl : DefaultManagementTtl;
                    }

                    if (ttl < MinTtlSeconds) ttl = MinTtlSeconds;
                    if (ttl > MaxTtlSeconds) ttl = MaxTtlSeconds;

                    return ttl;
                }
            }
        }
    }
}
