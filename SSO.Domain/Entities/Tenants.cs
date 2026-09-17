using SSO.Domain.Contract;
using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Domain.Entities
{
    public class Tenants : AuditableEntity<Guid>
    {
        public string Code { get; set; }        // acme, contoso
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string FaviconUrl { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? BillingAddress { get; set; }
        public TenantDatabaseMode DatabaseMode { get; set; }
        public string? DatabaseProvider { get; set; }
        public string? DatabaseName { get; set; }
        public string? ConnectionString { get; set; }

        /// <summary>Number of days after invoice due date before SSO access is blocked. Default: 7 days.</summary>
        public int GracePeriodDays { get; set; } = 7;
        public string Currency { get; set; } = "INR";

        //public Guid SubscriptionId { get; set; }
        public bool IsActive { get; set; }
        public string? BackgroundText { get; set; }
        public virtual ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
        public virtual ICollection<TenantClient> TenantClients { get; set; } = new List<TenantClient>();
    }
}
