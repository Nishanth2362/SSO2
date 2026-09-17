using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Responses.Features
{
    public class TenantResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; }        // acme, contoso
        public string Name { get; set; }

        public TenantDatabaseMode DatabaseMode { get; set; }
        public string? DatabaseProvider { get; set; }
        public string? DatabaseName { get; set; }
        public string? ConnectionString { get; set; }
        public bool IsActive { get; set; }
        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? BillingAddress { get; set; }
        public int GracePeriodDays { get; set; }
        public string Currency { get; set; }
        public bool AllowPublicRegistration { get; set; }
        public string? SubscriptionName { get; set; }
        public DateTime? SubscriptionExpiry { get; set; }
        public List<Guid>? ClientIds { get; set; }
    }
}
