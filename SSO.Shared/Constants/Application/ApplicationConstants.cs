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
    }
}
