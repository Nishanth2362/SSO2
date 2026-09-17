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
        Custom = 99
    }
}
