using OpenIddict.Abstractions;
using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Helper
{
    public static class OpenIdExtentions
    {
        public static string ToApplicationType(this ClientType clientType)
        {
            return clientType switch
            {
                ClientType.Spa => OpenIddictConstants.ApplicationTypes.Web,
                ClientType.Native => OpenIddictConstants.ApplicationTypes.Native,
                ClientType.Device => OpenIddictConstants.ApplicationTypes.Native,

                ClientType.Web => OpenIddictConstants.ApplicationTypes.Web,
                ClientType.Machine => OpenIddictConstants.ApplicationTypes.Web,

                _ => throw new ArgumentOutOfRangeException(nameof(clientType))
            };
        }

        public static bool RequiresClientSecret(this ClientType clientType)
        {
            return clientType switch
            {
                ClientType.Web => true,
                ClientType.Machine => true,
                _ => false
            };
        }

        public static bool RequiresPkce(this ClientType clientType)
        {
            return clientType switch
            {
                ClientType.Spa => true,
                ClientType.Native => true,
                ClientType.Device => true,
                _ => false
            };
        }
        public static string GetConsentType(this ClientType type)
        {
            return type switch
            {
                ClientType.Web => OpenIddictConstants.ConsentTypes.Implicit,
                ClientType.Machine => OpenIddictConstants.ConsentTypes.Implicit,
                ClientType.Device => OpenIddictConstants.ConsentTypes.Systematic,
                _ => OpenIddictConstants.ConsentTypes.Explicit
            };
        }
        public static string GetClientType(this ClientType type)
        {
            return type switch
            {
                ClientType.Spa => OpenIddictConstants.ClientTypes.Public,
                ClientType.Native => OpenIddictConstants.ClientTypes.Public,
                ClientType.Device => OpenIddictConstants.ClientTypes.Public,
                _ => OpenIddictConstants.ClientTypes.Confidential
            };
        }

        public static IEnumerable<string> BuildClientPermissions(ClientType type)
        {
            var p = new List<string>
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.Revocation
            };

            if (type == ClientType.Machine)
            {
                p.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
                p.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "api");
                p.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
                return p;
            }

            p.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            p.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
            p.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "openid");
            p.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "profile");
            p.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "email");
            p.Add(OpenIddictConstants.Permissions.Prefixes.Scope + "api");
            p.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            p.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            p.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            

            if (type is ClientType.Spa or ClientType.Native)
                p.Add(OpenIddictConstants.Permissions.Prefixes.GrantType + "pkce");

            if (type == ClientType.Device)
                p.Add(OpenIddictConstants.Permissions.GrantTypes.DeviceCode);

            if(type == ClientType.Web && RequiresClientSecret(type))
            {
                p.Add(OpenIddictConstants.Permissions.Endpoints.Introspection);
            }
            
            return p;
        }
    }
}
