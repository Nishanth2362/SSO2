using SSO.Application.Configuaration;
using SSO.Common.Wrapper;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface IRpapSettingsService
    {
        RpapSettings GetSettings();
        Task<Result<RpapSettings>> UpdateSettingsAsync(RpapSettings newSettings);
        Task<Result<RpapSettings>> ResetToDefaultsAsync();
        string ResolveLandingUrl(string? scope, string? requestedTargetUrl = null);
        int ResolveTtl(int? requestedTtlSeconds, string? scope);
        bool IsScopeAllowed(string? scope);
        bool IsActionAllowed(string? scope, string? controllerName, string? actionName);
        string GetFallbackUnauthorizedRedirectUrl();
    }
}
