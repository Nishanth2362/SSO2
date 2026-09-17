using SSO.Domain.Contract;
using System;

namespace SSO.Domain.Entities
{
    /// <summary>
    /// Database entity storing configurable token lifetimes for Access Token, Refresh Token, and Authorization Code.
    /// Can be configured globally (TenantId == null) or per-tenant.
    /// </summary>
    public class TokenLifetimeSetting : AuditableEntity<Guid>
    {
        /// <summary>Access Token validity in minutes (e.g., 60 = 1 hour).</summary>
        public int AccessTokenLifetimeMinutes { get; set; }

        /// <summary>Refresh Token validity in days (e.g., 14 = 14 days).</summary>
        public int RefreshTokenLifetimeDays { get; set; }

        /// <summary>Authorization Code validity in minutes (e.g., 5 = 5 minutes).</summary>
        public int AuthorizationCodeLifetimeMinutes { get; set; }

        /// <summary>Optional Tenant association (null = Global Default).</summary>
        public Guid? TenantId { get; set; }
    }
}
