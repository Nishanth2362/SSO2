namespace SSO.Domain.Enums
{
    public enum EmailTriggerEvent : byte
    {
        Registration = 0,
        PasswordReset = 1,
        EmailConfirmation = 2,
        SecurityAlert = 3,
        AccountLocked = 4,
        WelcomeEmail = 5,
        Invitation = 6,

        /// <summary>OTP code sent after successful password entry (2FA step).</summary>
        TwoFactorCode = 7,

        /// <summary>OTP code sent for passwordless Mobile OTP login flow.</summary>
        MobileOtpCode = 8,

        Custom = 99

    }
}
