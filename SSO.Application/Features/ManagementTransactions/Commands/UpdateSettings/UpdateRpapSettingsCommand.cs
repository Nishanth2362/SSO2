using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Services;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Commands.UpdateSettings
{
    public class UpdateRpapSettingsCommand : IRequest<Result<RpapSettings>>
    {
        // TTL Configuration
        public int DefaultProfileTtlSeconds { get; set; } = 300;
        public int DefaultManagementTtlSeconds { get; set; } = 900;
        public int MinTtlSeconds { get; set; } = 60;
        public int MaxTtlSeconds { get; set; } = 86400;

        // Routing & Landing URLs
        public string ProfileLandingUrl { get; set; } = "/Account/Profile";
        public string UsersLandingUrl { get; set; } = "/Home/Users";
        public string FallbackUnauthorizedRedirectUrl { get; set; } = "/Account/Profile";

        // Protocol Security Flags
        public bool AutoRevokeOnResultExchange { get; set; } = true;
        public bool RequireStrictLocalhostPort { get; set; } = false;
        public bool EnforceScopeAccess { get; set; } = true;
        public int ContextCookieExpiryDays { get; set; } = 7;

        // Scopes & Filter Allowed Actions
        public List<string> AllowedScopes { get; set; } = new();
        public List<string> ProfileScopeAllowedActions { get; set; } = new();
        public List<string> UsersScopeAllowedActions { get; set; } = new();
    }

    internal class UpdateRpapSettingsCommandHandler : IRequestHandler<UpdateRpapSettingsCommand, Result<RpapSettings>>
    {
        private readonly IRpapSettingsService _settingsService;

        public UpdateRpapSettingsCommandHandler(IRpapSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<Result<RpapSettings>> Handle(UpdateRpapSettingsCommand request, CancellationToken cancellationToken)
        {
            var model = new RpapSettings
            {
                DefaultProfileTtlSeconds = request.DefaultProfileTtlSeconds,
                DefaultManagementTtlSeconds = request.DefaultManagementTtlSeconds,
                MinTtlSeconds = request.MinTtlSeconds,
                MaxTtlSeconds = request.MaxTtlSeconds,
                ProfileLandingUrl = request.ProfileLandingUrl,
                UsersLandingUrl = request.UsersLandingUrl,
                FallbackUnauthorizedRedirectUrl = request.FallbackUnauthorizedRedirectUrl,
                AutoRevokeOnResultExchange = request.AutoRevokeOnResultExchange,
                RequireStrictLocalhostPort = request.RequireStrictLocalhostPort,
                EnforceScopeAccess = request.EnforceScopeAccess,
                ContextCookieExpiryDays = request.ContextCookieExpiryDays,
                AllowedScopes = request.AllowedScopes ?? new List<string>(),
                ProfileScopeAllowedActions = request.ProfileScopeAllowedActions ?? new List<string>(),
                UsersScopeAllowedActions = request.UsersScopeAllowedActions ?? new List<string>()
            };

            return await _settingsService.UpdateSettingsAsync(model);
        }
    }
}
