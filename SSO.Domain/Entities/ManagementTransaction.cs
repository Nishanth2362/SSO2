using SSO.Domain.Contract;
using SSO.Domain.Enums;
using System;

namespace SSO.Domain.Entities
{
    /// <summary>
    /// Represents a secure, backchannel-initiated, single-use Restricted Page Access transaction.
    /// Used for scoped access like profile:manage, users:manage, and users_roles:manage.
    /// </summary>
    public class ManagementTransaction : AuditableEntity<Guid>
    {
        public string ClientId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        
        /// <summary>Target scope: "profile:manage", "users:manage", "users_roles:manage", etc.</summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>SHA-256 hash of the single-use launch token presented by the browser.</summary>
        public string LaunchTokenHash { get; set; } = string.Empty;

        /// <summary>Target relative landing URL (e.g. /Account/Profile, /Home/Users).</summary>
        public string TargetUrl { get; set; } = string.Empty;

        /// <summary>Pre-validated client callback URL to return after transaction completion.</summary>
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>Optional state parameter passed by client for CSRF mitigation.</summary>
        public string? State { get; set; }

        /// <summary>Expiry timestamp for the launch token (default 5-15 mins from initiation).</summary>
        public DateTime ExpiresOn { get; set; }

        public bool IsConsumed { get; set; }
        public DateTime? ConsumedOn { get; set; }
        public string? ConsumedIpAddress { get; set; }
        public string? ConsumedUserAgent { get; set; }

        /// <summary>SHA-256 hash of the one-time completion result code returned to client callback.</summary>
        public string? ResultCodeHash { get; set; }
        public DateTime? ResultCodeExpiresOn { get; set; }
        public bool IsResultCodeConsumed { get; set; }
        public DateTime? ResultCodeConsumedOn { get; set; }

        public ManagementTransactionStatus Status { get; set; } = ManagementTransactionStatus.Pending;
        public string? RevokedReason { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string? RevokedBy { get; set; }

        /// <summary>JSON array of field names that were modified during this session (e.g. ["name", "email", "password"]).</summary>
        public string? ChangedFieldsJson { get; set; }
    }
}
