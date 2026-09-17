using SSO.Common.Constants.Localization;
using System.Globalization;

namespace SSO.WebApplication.Middlewares
{
    public class RequestCultureMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestCultureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var supportedCultures = LocalizationConstants.SupportedLanguages
                .Select(l => l.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var cultureQuery = context.Request.Query["culture"];
            if (TryGetSupportedCulture(cultureQuery, supportedCultures, out var queryCulture))
            {
                CultureInfo.CurrentCulture = queryCulture!;
                CultureInfo.CurrentUICulture = queryCulture!;
            }
            else if (TryGetSupportedCulture(context.Request.Headers["Accept-Language"].FirstOrDefault(), supportedCultures, out var headerCulture))
            {
                CultureInfo.CurrentCulture = headerCulture!;
                CultureInfo.CurrentUICulture = headerCulture!;
            }

            await _next(context);
        }

        private static bool TryGetSupportedCulture(string? rawCulture, HashSet<string> supportedCultures, out CultureInfo? culture)
        {
            culture = null;
            if (string.IsNullOrWhiteSpace(rawCulture))
            {
                return false;
            }

            var candidate = rawCulture.Split(',').FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(candidate) || !supportedCultures.Contains(candidate))
            {
                return false;
            }

            try
            {
                culture = new CultureInfo(candidate);
                return true;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}
