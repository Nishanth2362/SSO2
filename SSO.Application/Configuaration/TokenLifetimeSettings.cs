using System;

namespace SSO.Application.Configuaration
{
    public class TokenLifetimeSettings
    {
        /// <summary>Access Token validity in minutes (Default: 60 = 1 hour).</summary>
        public int AccessTokenLifetimeMinutes { get; set; }

        /// <summary>Refresh Token validity in days (Default: 14 = 14 days).</summary>
        public int RefreshTokenLifetimeDays { get; set; }

        /// <summary>Authorization Code validity in minutes (Default: 5 = 5 minutes).</summary>
        public int AuthorizationCodeLifetimeMinutes { get; set; }

        /// <summary>Optional Tenant ID (null = Global Default).</summary>
        public Guid? TenantId { get; set; }
    }
}
