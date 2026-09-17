using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SSO.Application.Requests.Identity
{
    public class TokenRequest : IValidatableObject
    {
        [Display(Name = "Email or username")]
        public string? UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        /// <summary>
        /// Current step in the multi-step login flow.
        /// 1 = identifier entry, 2 = password / mobile OTP, 3 = 2FA OTP after password
        /// </summary>
        public int Step { get; set; } = 1;

        public string? ReturnUrl { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; } = string.Empty;

        // ─── 2FA / OTP ───────────────────────────────────────────────────────────

        /// <summary>
        /// The 6-digit OTP submitted during Step 3 (2FA after password)
        /// or Step 2 of the Mobile OTP flow.
        /// </summary>
        public string? TwoFactorCode { get; set; }

        // ─── Mobile OTP flow ─────────────────────────────────────────────────────

        /// <summary>
        /// Email address or phone number used to look up the user in the Mobile OTP login flow.
        /// Only populated when LoginFlow == "mobile".
        /// </summary>
        public string? PhoneOrEmail { get; set; }

        /// <summary>
        /// Identifies which branch of the login flow is active.
        /// Values: "credentials" (default) | "mobile" (passwordless OTP).
        /// Only relevant when the client's AllowedLoginMethod == Both.
        /// </summary>
        public string? LoginFlow { get; set; } = "credentials";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var isMobileFlow = LoginFlow?.Equals("mobile", System.StringComparison.OrdinalIgnoreCase) == true;

            // Step 1 — require an identifier (either username or phone/email for mobile)
            if (Step == 1)
            {
                if (isMobileFlow && string.IsNullOrWhiteSpace(PhoneOrEmail))
                {
                    yield return new ValidationResult(
                        "Email or phone number is required.",
                        new[] { nameof(PhoneOrEmail) });
                }
                else if (!isMobileFlow && string.IsNullOrWhiteSpace(UserName))
                {
                    yield return new ValidationResult(
                        "Email or username required.",
                        new[] { nameof(UserName) });
                }
            }

            // Step 2 — credentials flow requires password; mobile flow requires OTP
            if (Step == 2 && !isMobileFlow && string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult(
                    "Password required.",
                    new[] { nameof(Password) });
            }

            if (Step == 2 && isMobileFlow && string.IsNullOrWhiteSpace(TwoFactorCode))
            {
                yield return new ValidationResult(
                    "Verification code required.",
                    new[] { nameof(TwoFactorCode) });
            }

            // Step 3 — 2FA OTP after successful password
            if (Step == 3 && string.IsNullOrWhiteSpace(TwoFactorCode))
            {
                yield return new ValidationResult(
                    "Verification code required.",
                    new[] { nameof(TwoFactorCode) });
            }
        }
    }
}
