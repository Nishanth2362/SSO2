using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Domain.Entities
{
    public class TenantLicense : AuditableEntity<Guid>
    {
        public Guid TenantId { get; set; }
        public Guid ClientApplicationId { get; set; }

        public string LicenseKey { get; set; } = null!;

        public DateTime ValidFromUtc { get; set; }
        public DateTime ValidToUtc { get; set; }

        public int MaxUsers { get; set; }
        public int MaxConcurrentUsers { get; set; }

        public string FeaturesJson { get; set; } = null!; // feature flags

        public bool IsRevoked { get; set; }
    }
}
