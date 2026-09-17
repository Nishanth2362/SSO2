using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Infrastructure.Models
{
    public class DefaultSetting
    {
        public string TenantCode { get; set; }
        public string TenantName { get; set; }
        public string TenantLogo { get; set; }
        public TenantDatabaseMode DatabaseMode { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public string BillingAddress { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string SubscriptionName { get; set; }
        public string FavIconUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ClientName { get; set; }
        public ClientType ClientType { get; set; }
        public string ScopesName { get; set; }
        public string DefaultUserName { get; set; }
        public string DefaultPassword { get; set; }
        public string DefaultEmail { get; set; }
        public string DefaultRole { get; set; }
        public string DefaultPhoneNumber { get; set; }
        public string PostLogoutRedirectUris { get; set; } = string.Empty;
        public string RedirectUris { get; set; } = string.Empty;
    }
}
