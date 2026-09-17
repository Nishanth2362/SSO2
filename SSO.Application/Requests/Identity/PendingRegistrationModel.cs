using System;

namespace SSO.Application.Requests.Identity
{
    /// <summary>
    /// Ephemeral registration state held in memory during the 10-minute email OTP verification window.
    /// Accounts are only committed to the database after the 6-digit email OTP is confirmed.
    /// </summary>
    public class PendingRegistrationModel
    {
        public string Token { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string? ReturnUrl { get; set; }
        public string OtpHash { get; set; } = string.Empty;
        public DateTime OtpExpiryUtc { get; set; }
        public int FailedAttempts { get; set; } = 0;
        public DateTime LastResendUtc { get; set; } = DateTime.UtcNow;
        public bool IsOtpVerified { get; set; } = false;
    }
}
