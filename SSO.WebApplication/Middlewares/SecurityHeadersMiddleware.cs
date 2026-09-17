using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SSO.WebApplication.Middlewares
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(RequestDelegate next, Microsoft.Extensions.Configuration.IConfiguration configuration, IWebHostEnvironment environment)
        {
            _next = next;
            _configuration = configuration;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var section = _configuration.GetSection("SecuritySettings");

            // Generate a unique nonce for this request
            var nonce = GenerateNonce();
            context.Items["CSPNonce"] = nonce;

            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                // 1. Prevent Clickjacking
                headers["X-Frame-Options"] = section["X-Frame-Options"] ?? "SAMEORIGIN";

                // 2. Prevent MIME type sniffing
                headers["X-Content-Type-Options"] = section["X-Content-Type-Options"] ?? "nosniff";

                // 3. Prevent XSS attacks in older browsers
                headers["X-XSS-Protection"] = section["X-XSS-Protection"] ?? "1; mode=block";

                // 4. Referrer-Policy
                headers["Referrer-Policy"] = section["Referrer-Policy"] ?? "strict-origin-when-cross-origin";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";

                // 5. Content-Security-Policy (CSP)
                headers["Content-Security-Policy"] = BuildCsp(context, nonce);

                // 6. Permissions-Policy
                headers["Permissions-Policy"] = section["Permissions-Policy"] ?? "camera=(), microphone=(), geolocation=(), payment=()";
                return Task.CompletedTask;
            });

            await _next(context);
        }
        private string GenerateNonce()
        {
            byte[] byteArray = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(byteArray);
            }
            return Convert.ToBase64String(byteArray);
        }

        private string BuildCsp(HttpContext context, string nonce)
        {
            var isDevelopment = _environment.IsDevelopment();
            var connectSrc = isDevelopment
                ? "connect-src 'self' https: http://localhost:* ws://localhost:* wss://localhost:* wss: ws:;"
                : "connect-src 'self' https: wss: ws:;";
            var formAction = BuildFormAction(context, isDevelopment);
            const string openIddictFormPostScriptHash = " 'sha256-j7OoGArf6XW6YY4cAyS3riSSvrJRqpSi1fOF9vQ5SrI='";

            return string.Join(" ",
                "default-src 'self';",
                "base-uri 'self';",
                "object-src 'none';",
                "frame-ancestors 'self';",
                formAction,
                $"script-src 'self' 'nonce-{nonce}'{openIddictFormPostScriptHash} https://cdn.jsdelivr.net https://cdn.datatables.net;",
                "script-src-attr 'unsafe-inline';",
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net https://cdn.datatables.net;",
                "img-src 'self' data: https:;",
                "font-src 'self' data: https://fonts.gstatic.com;",
                connectSrc,
                "worker-src 'self';",
                "manifest-src 'self';",
                "frame-src 'self';",
                "upgrade-insecure-requests;");
        }

        private string BuildFormAction(HttpContext context, bool isDevelopment)
        {
            var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "'self'"
            };

            foreach (var origin in GetConfiguredOrigins())
            {
                allowedOrigins.Add(origin);
            }

            if (TryGetAuthorizeRedirectOrigin(context, out var redirectOrigin))
            {
                allowedOrigins.Add(redirectOrigin);
            }

            if (isDevelopment)
            {
                allowedOrigins.Add("https://localhost:*");
                allowedOrigins.Add("http://localhost:*");
            }

            return $"form-action {string.Join(" ", allowedOrigins)};";
        }

        private IEnumerable<string> GetConfiguredOrigins()
        {
            var configuredOrigins = _configuration.GetSection("SecuritySettings:AllowedCorsOrigins").Get<string[]>() ?? Array.Empty<string>();
            foreach (var origin in configuredOrigins)
            {
                if (TryGetOrigin(origin, out var normalizedOrigin))
                {
                    yield return normalizedOrigin;
                }
            }

            if (TryGetOrigin(_configuration["AppConfiguration:ApplicationUrl"], out var applicationOrigin))
            {
                yield return applicationOrigin;
            }
        }

        private static bool TryGetAuthorizeRedirectOrigin(HttpContext context, out string origin)
        {
            origin = string.Empty;

            if (!context.Request.Path.Equals("/connect/authorize", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var redirectUri = context.Request.Query["redirect_uri"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(redirectUri) && context.Request.HasFormContentType)
            {
                redirectUri = context.Request.Form["redirect_uri"].FirstOrDefault();
            }

            return TryGetOrigin(redirectUri, out origin);
        }

        private static bool TryGetOrigin(string? uriValue, out string origin)
        {
            origin = string.Empty;
            if (!Uri.TryCreate(uriValue, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            origin = uri.GetLeftPart(UriPartial.Authority);
            return true;
        }
    }
}
