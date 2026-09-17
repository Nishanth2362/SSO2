using SSO.Domain.Entities;
using SSO.Domain.Enums;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    /// <summary>
    /// Handles all OTP-based authentication operations including generation, delivery,
    /// and time-limited validation for both 2FA (post-password) and Mobile OTP (passwordless) flows.
    /// </summary>
    public interface ITwoFactorService
    {
        /// <summary>
        /// Checks whether the given client requires 2FA for all logins.
        /// Returns true when <see cref="Domain.Entities.ApplicationClient.Require2FA"/> is set.
        /// </summary>
        Task<bool> Is2FARequiredAsync(string clientId);

        /// <summary>
        /// Generates a cryptographically secure 6-digit OTP, stores its SHA-256 hash
        /// and expiry (10 minutes) in <see cref="ApplicationUser.TwoFactorSecret"/>,
        /// and sends it to the user via the appropriate email template.
        /// </summary>
        /// <param name="user">The target user — must have a confirmed email address.</param>
        /// <param name="branding">Optional tenant branding for email personalisation.</param>
        /// <param name="trigger">
        /// <see cref="EmailTriggerEvent.TwoFactorCode"/> for 2FA after password, or
        /// <see cref="EmailTriggerEvent.MobileOtpCode"/> for the passwordless flow.
        /// </param>
        Task SendOtpAsync(ApplicationUser user, Domain.Entities.Tenants? branding, EmailTriggerEvent trigger);

        /// <summary>
        /// Validates the submitted OTP against the stored hash and expiry.
        /// Clears <see cref="ApplicationUser.TwoFactorSecret"/> on a successful match
        /// to prevent replay attacks.
        /// </summary>
        Task<OtpValidationResult> ValidateOtpAsync(ApplicationUser user, string? submittedCode);
    }

    /// <summary>Result of an OTP validation attempt.</summary>
    /// <param name="IsValid">True when the code matched and had not expired.</param>
    /// <param name="Error">Human-readable error message when <paramref name="IsValid"/> is false.</param>
    public record OtpValidationResult(bool IsValid, string? Error = null);
}
