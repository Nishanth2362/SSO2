using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Responses.Features
{
    public record ClientResponse
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string Scopes { get; set; }
        public string AppClientType { get; set; }
        public string ClientType { get; set; }
        public string? Audience { get; set; }
        public bool? IsOnline { get; set; }
        public string? LastCheckTime { get; set; }

        /// <summary>Whether this client enforces OTP as a second factor.</summary>
        public bool Require2FA { get; set; }

        /// <summary>The allowed login method string: "CredentialsOnly" | "MobileOtpOnly" | "Both".</summary>
        public string AllowedLoginMethod { get; set; } = "CredentialsOnly";

        /// <summary>Whether this client allows public self-registration from the login page.</summary>
        public bool AllowPublicRegistration { get; set; }

        /// <summary>The default role assigned to self-registered users under this client.</summary>
        public string? DefaultRoleName { get; set; } = "End User";

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public string? IPAddress { get; set; }
        public bool IsDeleted { get; set; }
    }

    public record ClientHealthStatus(bool IsOnline, DateTime LastCheck);
}
