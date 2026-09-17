using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    /// <summary>
    /// Service providing multi-layered defense against automated bots, credential abuse,
    /// and registration spam (Honeypot, Cryptographic Time-Lock, Disposable Email Blocker).
    /// </summary>
    public interface IAntiBotSecurityService
    {
        /// <summary>Generates an HMAC-signed timestamp token to embed in public registration and login forms.</summary>
        string GenerateTimeLockToken();

        /// <summary>
        /// Validates that the form was filled by a human (not submitted in under 2.5 seconds by an automated script)
        /// and has not expired (within 30 minutes).
        /// </summary>
        bool ValidateTimeLock(string? token, out string? errorMessage);

        /// <summary>Returns true if the hidden decoy honeypot field was filled by a bot.</summary>
        bool IsHoneypotTriggered(string? honeypotValue);

        /// <summary>Checks whether an email has a strictly valid syntax format before any DNS or DB lookups.</summary>
        bool IsValidEmailSyntax(string? email);

        /// <summary>Returns true if the email address belongs to a known temporary/throwaway disposable provider.</summary>
        bool IsDisposableEmail(string? email);

        /// <summary>
        /// Validates that the email domain resolves via DNS (has valid A/AAAA/MX records) to prevent sending emails to non-existent domains.
        /// Uses an internal cache to avoid redundant DNS lookups.
        /// </summary>
        Task<bool> HasValidDnsMxRecordAsync(string? email);

        /// <summary>Returns the total number of disposable domains loaded in memory.</summary>
        int GetDisposableDomainsCount();
    }
}
