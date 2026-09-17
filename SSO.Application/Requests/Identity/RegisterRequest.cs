using System;
using System.ComponentModel.DataAnnotations;

namespace SSO.Application.Requests.Identity
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(256, ErrorMessage = "Email address cannot exceed 256 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid mobile number.")]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Mobile number must be between 10 and 15 digits.")]
        public string PhoneNumber { get; set; } = string.Empty;

        // ── Step 3: Password Credentials ──────────────────────────────────
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }

        public string? ReturnUrl { get; set; }
        
        [Required(ErrorMessage = "Client ID is required.")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tenant ID is required.")]
        public Guid TenantId { get; set; }

        // ── Multi-Step Email OTP Verification ──────────────────────────────
        /// <summary>Step 1: Enter Details; Step 2: Verify Email OTP.</summary>
        public int Step { get; set; } = 1;

        /// <summary>Transient token referencing pending registration in cache.</summary>
        public string? RegistrationToken { get; set; }

        /// <summary>6-digit OTP code submitted in Step 2.</summary>
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits.")]
        public string? VerificationCode { get; set; }

        // ── Anti-Bot & Attacker Defenses ──────────────────────────────────
        /// <summary>Honeypot trap: hidden off-screen field. Must be empty; if populated, submission is a bot.</summary>
        public string? Website_Hp { get; set; }

        /// <summary>Cryptographically signed time token to detect instant (<2.5s) automated script submissions.</summary>
        public string? FormTimestampToken { get; set; }

    }

    public class ResendRegistrationOtpRequest
    {
        [Required]
        public string RegistrationToken { get; set; } = string.Empty;
    }
}
