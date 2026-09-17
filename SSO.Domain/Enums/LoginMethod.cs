namespace SSO.Domain.Enums
{
    /// <summary>
    /// Controls which login flows are available on the login page for a specific client application.
    /// </summary>
    public enum LoginMethod : byte
    {
        /// <summary>Users must authenticate with email/username and password.</summary>
        CredentialsOnly = 0,

        /// <summary>Passwordless login via email OTP (phone number or email as identifier).</summary>
        MobileOtpOnly = 1,

        /// <summary>Users may choose either credentials or mobile OTP on the login page.</summary>
        Both = 2
    }
}
