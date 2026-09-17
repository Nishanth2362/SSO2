using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Infrastructure.Contexts;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Infrastructure.Services
{
    /// <summary>
    /// Implements OTP generation, secure storage, delivery, and time-limited validation
    /// for both the 2FA (post-password) and Mobile OTP (passwordless) login flows.
    ///
    /// Security properties:
    ///   • OTP is cryptographically random (RandomNumberGenerator).
    ///   • Stored as SHA-256 hash — never the plaintext code.
    ///   • 10-minute hard expiry enforced server-side.
    ///   • Consumed on first successful use (replay-proof).
    ///   • Failures are not persisted to TwoFactorSecret — the window stays open for retry.
    /// </summary>
    public class TwoFactorService : ITwoFactorService
    {
        private const int OtpExpiryMinutes = 10;

        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMailService _mailService;
        private readonly IEmailTemplateService _emailTemplateService;

        public TwoFactorService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IMailService mailService,
            IEmailTemplateService emailTemplateService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _mailService = mailService;
            _emailTemplateService = emailTemplateService;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Is2FARequiredAsync
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<bool> Is2FARequiredAsync(string clientId)
        {
            return await _dbContext.Clients
                .AsNoTracking()
                .Where(c => c.ClientId == clientId)
                .Select(c => c.Require2FA)
                .FirstOrDefaultAsync();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // SendOtpAsync
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task SendOtpAsync(
            ApplicationUser user,
            Domain.Entities.Tenants? branding,
            EmailTriggerEvent trigger)
        {
            // 1. Generate a 6-digit cryptographically secure OTP
            var otpCode = GenerateOtp();

            // 2. Hash + store with expiry and initial attempt count (0)
            var hash   = ComputeHash(otpCode);
            var expiry = DateTimeOffset.UtcNow.AddMinutes(OtpExpiryMinutes).ToUnixTimeSeconds();
            user.TwoFactorSecret = $"{hash}|{expiry}|0";
            await _userManager.UpdateAsync(user);

            // 3. Build email and dispatch asynchronously (fire-and-forget via Hangfire)
            var tokens = new Dictionary<string, string>
            {
                ["UserName"]   = user.Name ?? user.UserName ?? "User",
                ["Email"]      = user.Email ?? string.Empty,
                ["OtpCode"]    = otpCode,
                ["ExpiryMins"] = OtpExpiryMinutes.ToString(),
                ["AppName"]    = branding?.Name ?? "SSO",
                ["TenantName"] = branding?.Name ?? "SSO",
                ["LogoUrl"]    = branding?.LogoUrl ?? string.Empty
            };

            var template = await _emailTemplateService.RenderAsync(trigger, tokens);

            var subject = trigger == EmailTriggerEvent.TwoFactorCode
                ? $"Your verification code — {otpCode}"
                : $"Your one-time login code — {otpCode}";

            var body = !string.IsNullOrWhiteSpace(template?.Body)
                ? template.Value.Body
                : BuildFallbackBody(otpCode, trigger);

            var mailRequest = new MailRequest
            {
                To      = new List<string> { user.Email! },
                Subject = !string.IsNullOrWhiteSpace(template?.Subject) ? template.Value.Subject : subject,
                Body    = body
            };

            BackgroundJob.Enqueue(() => _mailService.SendAsync(mailRequest));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ValidateOtpAsync
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<OtpValidationResult> ValidateOtpAsync(
            ApplicationUser user,
            string? submittedCode)
        {
            if (string.IsNullOrWhiteSpace(submittedCode))
                return new OtpValidationResult(false, "Verification code is required.");

            if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                return new OtpValidationResult(false, "No active verification code. Please request a new one.");

            // Parse stored secret: hash|expiry|attempts
            var parts = user.TwoFactorSecret.Split('|');
            if (parts.Length < 2 || !long.TryParse(parts[1], out var expiryUnix))
                return new OtpValidationResult(false, "Verification code is invalid. Please request a new one.");

            int attempts = 0;
            if (parts.Length >= 3)
            {
                int.TryParse(parts[2], out attempts);
            }

            if (attempts >= 5)
            {
                user.TwoFactorSecret = null;
                await _userManager.UpdateAsync(user);
                return new OtpValidationResult(false, "Too many failed attempts. This verification code has been invalidated. Please request a new one.");
            }

            // Check expiry first (prevents timing oracle on expired codes)
            var expiryUtc = DateTimeOffset.FromUnixTimeSeconds(expiryUnix).UtcDateTime;
            if (DateTime.UtcNow > expiryUtc)
            {
                // Clear the expired secret
                user.TwoFactorSecret = null;
                await _userManager.UpdateAsync(user);
                return new OtpValidationResult(false, "Verification code has expired. Please request a new one.");
            }

            // Constant-time hash comparison to prevent timing attacks
            var storedHash    = parts[0];
            var submittedHash = ComputeHash(submittedCode.Trim());
            var isMatch       = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedHash),
                Encoding.UTF8.GetBytes(submittedHash));

            if (!isMatch)
            {
                attempts++;
                if (attempts >= 5)
                {
                    user.TwoFactorSecret = null;
                    await _userManager.UpdateAsync(user);
                    return new OtpValidationResult(false, "Too many failed attempts. This verification code has been invalidated. Please request a new one.");
                }

                user.TwoFactorSecret = $"{storedHash}|{expiryUnix}|{attempts}";
                await _userManager.UpdateAsync(user);
                var remaining = 5 - attempts;
                return new OtpValidationResult(false, $"Invalid verification code. {remaining} attempt(s) remaining.");
            }

            // Success — invalidate the OTP to prevent replay
            user.TwoFactorSecret = null;
            await _userManager.UpdateAsync(user);

            return new OtpValidationResult(true);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Generates a 6-digit OTP using a cryptographically secure RNG.</summary>
        private static string GenerateOtp()
        {
            // RandomNumberGenerator.GetInt32 is uniform and unbiased
            var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000);
            return code.ToString("D6");
        }

        /// <summary>Returns the uppercase hex SHA-256 digest of <paramref name="input"/>.</summary>
        private static string ComputeHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes); // uppercase hex, available in .NET 5+
        }

        private static string BuildFallbackBody(string otpCode, EmailTriggerEvent trigger)
        {
            return trigger == EmailTriggerEvent.TwoFactorCode
                ? $"<p>Your two-factor authentication code is: <strong style='font-size:24px;letter-spacing:4px'>{otpCode}</strong></p>"
                  + $"<p>This code expires in {OtpExpiryMinutes} minutes. Do not share it with anyone.</p>"
                : $"<p>Your one-time login code is: <strong style='font-size:24px;letter-spacing:4px'>{otpCode}</strong></p>"
                  + $"<p>This code expires in {OtpExpiryMinutes} minutes.</p>";
        }
    }
}
