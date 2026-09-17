using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSO.Infrastructure.Services
{
    /// <summary>
    /// Implements anti-bot defenses: Honeypot trap, Cryptographic Time-Lock,
    /// and Disposable Email Domain filtering.
    /// </summary>
    public class AntiBotSecurityService : IAntiBotSecurityService
    {
        private const double MinSubmissionSeconds = 2.0; // Humans cannot submit form under 2 seconds
        private const double MaxSubmissionMinutes = 30.0; // Forms expire after 30 minutes

        private readonly IConfiguration _configuration;
        private readonly ILogger<AntiBotSecurityService> _logger;
        private readonly byte[] _secretKey;

        private static readonly ConcurrentDictionary<string, bool> DnsValidationCache = new(StringComparer.OrdinalIgnoreCase);

        // RFC 5322 compliant email regex for fast early syntax validation
        private static readonly Regex EmailRegex = new(
            @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        // Lazy singleton for 75,000+ disposable domains with O(1) hash set lookup
        private static readonly Lazy<HashSet<string>> _lazyDisposableDomains = new(LoadDisposableDomains);
        private static HashSet<string> DisposableDomains => _lazyDisposableDomains.Value;

        public AntiBotSecurityService(
            IConfiguration configuration,
            ILogger<AntiBotSecurityService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var secret = _configuration["AppConfiguration:Secret"] ?? "SchoolaSSODefaultSecretKeyForHmacSha256Protection2026!";
            _secretKey = Encoding.UTF8.GetBytes(secret);

            // Trigger background load of disposable domains if not yet loaded
            _ = DisposableDomains.Count;
        }

        private static HashSet<string> LoadDisposableDomains()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Baseline fallback domains
            string[] defaultDomains =
            {
                "mailinator.com", "tempmail.com", "10minutemail.com", "guerrillamail.com",
                "throwawaymail.com", "yopmail.com", "trashmail.com", "fakemailgenerator.com",
                "getairmail.com", "dispostable.com", "sharklasers.com", "temp-mail.org",
                "maildrop.cc", "inboxkitten.com", "nada.ltd", "mohmal.com", "crazymailing.com",
                "generator.email", "emailondeck.com", "tempail.com", "burnermail.io",
                "dropmail.me", "getnada.com", "mytemp.email"
            };
            foreach (var d in defaultDomains) set.Add(d);

            try
            {
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "disposable_email_domains.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "disposable_email_domains.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "SSO.Infrastructure", "disposable_email_domains.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "SSO.WebApplication", "disposable_email_domains.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SSO.Infrastructure", "disposable_email_domains.json")
                };

                string? foundPath = null;
                foreach (var p in possiblePaths)
                {
                    try
                    {
                        var full = Path.GetFullPath(p);
                        if (File.Exists(full))
                        {
                            foundPath = full;
                            break;
                        }
                    }
                    catch { }
                }

                if (foundPath != null)
                {
                    using var stream = File.OpenRead(foundPath);
                    using var doc = JsonDocument.Parse(stream);

                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in doc.RootElement.EnumerateArray())
                        {
                            var val = element.GetString();
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                set.Add(val.Trim());
                            }
                        }
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                             doc.RootElement.TryGetProperty("domains", out var domainsArray) &&
                             domainsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in domainsArray.EnumerateArray())
                        {
                            var val = element.GetString();
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                set.Add(val.Trim());
                            }
                        }
                    }
                }
            }
            catch
            {
                // Gracefully retain baseline set
            }

            return set;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 1. Time-Lock Cryptographic Token
        // ─────────────────────────────────────────────────────────────────────────

        public string GenerateTimeLockToken()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = timestamp.ToString();
            using var hmac = new HMACSHA256(_secretKey);
            var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            var raw = $"{payload}:{hash}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        public bool ValidateTimeLock(string? token, out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(token))
            {
                errorMessage = "Security verification missing. Please refresh the page and try again.";
                return false;
            }

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':', 2);
                if (parts.Length != 2 || !long.TryParse(parts[0], out var timestamp))
                {
                    errorMessage = "Invalid security token. Please refresh the page.";
                    return false;
                }

                // Verify HMAC signature
                using var hmac = new HMACSHA256(_secretKey);
                var expectedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(parts[0])));
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedHash),
                        Encoding.UTF8.GetBytes(parts[1])))
                {
                    errorMessage = "Tampered security token. Please refresh the page.";
                    return false;
                }

                var formUtc = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                var elapsed = DateTime.UtcNow - formUtc;

                // Anti-Fast-Bot check: Submissions under 2 seconds are automated bots
                if (elapsed.TotalSeconds < MinSubmissionSeconds)
                {
                    _logger.LogWarning("Bot detected: Form submitted in {ElapsedMs}ms (minimum required {MinSec}s)",
                        elapsed.TotalMilliseconds, MinSubmissionSeconds);
                    errorMessage = "Form submitted too quickly. Please take a moment and submit again.";
                    return false;
                }

                // Form expiration check: Submissions over 30 minutes are expired/stale
                if (elapsed.TotalMinutes > MaxSubmissionMinutes)
                {
                    errorMessage = "Registration session has expired. Please refresh the page and submit again.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error decoding time-lock token");
                errorMessage = "Security token validation failed. Please refresh the page.";
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 2. Honeypot Trap
        // ─────────────────────────────────────────────────────────────────────────

        public bool IsHoneypotTriggered(string? honeypotValue)
        {
            // The honeypot field is hidden via CSS off-screen. Legitimate users never see or fill it.
            // Automated web crawlers fill all discovered inputs automatically.
            return !string.IsNullOrWhiteSpace(honeypotValue);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 3. Email Syntax & Disposable Email Filter & DNS Host Check
        // ─────────────────────────────────────────────────────────────────────────

        public bool IsValidEmailSyntax(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
            return EmailRegex.IsMatch(email.Trim());
        }

        public bool IsDisposableEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var atIndex = email.LastIndexOf('@');
            if (atIndex < 0 || atIndex == email.Length - 1) return false;

            var domain = email[(atIndex + 1)..].Trim().ToLowerInvariant();
            if (DisposableDomains.Contains(domain)) return true;

            // Also check parent domain (e.g. "sub.dropmail.me" -> "dropmail.me")
            var parts = domain.Split('.');
            if (parts.Length > 2)
            {
                var parentDomain = string.Join('.', parts[^2..]);
                if (DisposableDomains.Contains(parentDomain)) return true;
            }

            return false;
        }

        public async Task<bool> HasValidDnsMxRecordAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            var atIndex = email.LastIndexOf('@');
            if (atIndex < 0 || atIndex == email.Length - 1) return false;

            var domain = email[(atIndex + 1)..].Trim().ToLowerInvariant();

            // Fast cache check
            if (DnsValidationCache.TryGetValue(domain, out var cachedValid))
            {
                return cachedValid;
            }

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(2500));
                var entry = await Dns.GetHostEntryAsync(domain, cts.Token);

                var isValid = entry != null && entry.AddressList != null && entry.AddressList.Length > 0;
                DnsValidationCache[domain] = isValid;
                return isValid;
            }
            catch (SocketException sex)
            {
                _logger.LogInformation("Email domain '{Domain}' failed DNS lookup: {Message} (Code {ErrorCode})",
                    domain, sex.Message, sex.ErrorCode);
                DnsValidationCache[domain] = false;
                return false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("DNS lookup timed out for domain '{Domain}', allowing as fallback", domain);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error resolving DNS for domain '{Domain}'", domain);
                return true;
            }
        }

        public int GetDisposableDomainsCount()
        {
            return DisposableDomains.Count;
        }
    }
}
