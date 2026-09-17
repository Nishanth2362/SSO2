namespace SSO.Domain.Enums
{
    /// <summary>
    /// Classifies authentication-related security events for auditing and monitoring.
    /// </summary>
    public enum SecurityEventType : byte
    {
        /// <summary>User successfully authenticated.</summary>
        LoginSuccess = 0,

        /// <summary>Incorrect password submitted.</summary>
        PasswordFailed = 1,

        /// <summary>Incorrect or already-used OTP submitted.</summary>
        OtpFailed = 2,

        /// <summary>Account was locked out after repeated failures.</summary>
        AccountLocked = 3,

        /// <summary>OTP was submitted after its 10-minute expiry window.</summary>
        OtpExpired = 4,

        /// <summary>Access control validation rejected the user (tenant, subscription, consent).</summary>
        AccessDenied = 5,

        /// <summary>Reserved for future anomaly detection (impossible travel, etc.).</summary>
        SuspiciousActivity = 6
    }
}
