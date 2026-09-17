using System;

namespace SSO.Application.Interfaces.Services
{
    /// <summary>
    /// DTO returned by <see cref="ISecurityEventService"/> for display in the
    /// SSO Security Management page and per-user history views.
    /// </summary>
    public class SecurityEventResponse
    {
        public long Id { get; set; }

        // ─── Who ──────────────────────────────────────────────────────────────────
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public string ClientId { get; set; } = string.Empty;

        // ─── What ─────────────────────────────────────────────────────────────────
        public string EventType { get; set; } = string.Empty;
        public int AttemptNumber { get; set; }
        public bool IsBlocked { get; set; }

        // ─── Network / Geo ────────────────────────────────────────────────────────
        public string? IpAddress { get; set; }
        public string? CountryCode { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }

        // ─── Device ───────────────────────────────────────────────────────────────
        public string? DeviceType { get; set; }
        public string? BrowserName { get; set; }
        public string? OsName { get; set; }
        public string? UserAgent { get; set; }

        // ─── When ─────────────────────────────────────────────────────────────────
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>Local time formatted for display (IST-aware via browser JS, server provides UTC).</summary>
        public string OccurredAtDisplay => OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss");

        // ─── UI helpers ───────────────────────────────────────────────────────────

        /// <summary>Flag emoji derived from two-letter ISO country code.</summary>
        public string? CountryFlag => string.IsNullOrEmpty(CountryCode)
            ? null
            : string.Concat(CountryCode.ToUpperInvariant().Select(c =>
                char.ConvertFromUtf32(c + 0x1F1A5)));

        /// <summary>CSS class for row color coding in the security grid.</summary>
        public string RowClass => EventType switch
        {
            "LoginSuccess"    => "row-success",
            "AccountLocked"   => "row-blocked",
            "SuspiciousActivity" => "row-suspicious",
            _                 => "row-failure"
        };
    }
}
