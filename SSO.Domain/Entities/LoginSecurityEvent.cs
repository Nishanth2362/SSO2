using SSO.Domain.Enums;
using System;

namespace SSO.Domain.Entities
{
    /// <summary>
    /// Records a security-relevant authentication event for auditing and threat monitoring.
    /// Captured on: failed password, failed OTP, account lockout, access denial, successful login.
    /// </summary>
    public class LoginSecurityEvent
    {
        /// <summary>Auto-incrementing surrogate key.</summary>
        public long Id { get; set; }

        /// <summary>The authenticated user's ID. Null when the username does not match any account.</summary>
        public Guid? UserId { get; set; }

        /// <summary>The raw identifier submitted at the login prompt (email, username, or phone).</summary>
        public string? UserName { get; set; }

        /// <summary>The OpenIddict client_id of the application the user was signing into.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Classification of the event.</summary>
        public SecurityEventType EventType { get; set; }

        // ─── Network ───────────────────────────────────────────────────────────────

        /// <summary>Client IP address (respects X-Forwarded-For via proxy).</summary>
        public string? IpAddress { get; set; }

        // ─── GeoIP (resolved from IP via MaxMind GeoLite2) ────────────────────────

        /// <summary>Two-letter ISO country code, e.g. "IN", "US".</summary>
        public string? CountryCode { get; set; }

        /// <summary>Human-readable country name, e.g. "India".</summary>
        public string? Country { get; set; }

        /// <summary>City name resolved from IP.</summary>
        public string? City { get; set; }

        // ─── Device / Browser ─────────────────────────────────────────────────────

        /// <summary>Full User-Agent string from the HTTP request header.</summary>
        public string? UserAgent { get; set; }

        /// <summary>"Desktop" | "Mobile" | "Tablet" | "Unknown" — parsed from UA.</summary>
        public string? DeviceType { get; set; }

        /// <summary>Browser family name, e.g. "Chrome", "Safari", "Firefox".</summary>
        public string? BrowserName { get; set; }

        /// <summary>Operating system name, e.g. "Windows 11", "Android", "iOS".</summary>
        public string? OsName { get; set; }

        // ─── Event metadata ───────────────────────────────────────────────────────

        /// <summary>Consecutive failure count (1-based). Resets to 0 on successful login.</summary>
        public int AttemptNumber { get; set; }

        /// <summary>True when this event caused or occurred during a lockout state.</summary>
        public bool IsBlocked { get; set; }

        /// <summary>UTC timestamp of the event.</summary>
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    }
}
