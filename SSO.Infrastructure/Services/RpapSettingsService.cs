using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Application;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSO.Infrastructure.Services
{
    public class RpapSettingsService : IRpapSettingsService
    {
        private readonly object _lock = new();
        private RpapSettings _settings;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RpapSettingsService> _logger;
        private bool _isLoadedFromDb = false;

        public RpapSettingsService(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<RpapSettingsService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = new RpapSettings();

            try
            {
                var section = configuration.GetSection("RpapSettings");
                if (section.Exists())
                {
                    var configured = section.Get<RpapSettings>();
                    if (configured != null)
                    {
                        _settings = configured;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to bind RpapSettings from configuration. Using system defaults.");
            }
        }

        private void EnsureLoadedFromDb()
        {
            if (_isLoadedFromDb) return;

            lock (_lock)
            {
                if (_isLoadedFromDb) return;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var dbSetting = dbContext.RpapSettings.AsNoTracking().FirstOrDefault(s => s.TenantId == null);
                    if (dbSetting != null)
                    {
                        _settings = MapFromEntity(dbSetting);
                    }
                    _isLoadedFromDb = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load RpapSettings from database. Falling back to defaults.");
                    _isLoadedFromDb = true;
                }
            }
        }

        public RpapSettings GetSettings()
        {
            EnsureLoadedFromDb();

            lock (_lock)
            {
                return new RpapSettings
                {
                    DefaultProfileTtlSeconds = _settings.DefaultProfileTtlSeconds,
                    DefaultManagementTtlSeconds = _settings.DefaultManagementTtlSeconds,
                    MinTtlSeconds = _settings.MinTtlSeconds,
                    MaxTtlSeconds = _settings.MaxTtlSeconds,
                    ProfileLandingUrl = _settings.ProfileLandingUrl,
                    UsersLandingUrl = _settings.UsersLandingUrl,
                    FallbackUnauthorizedRedirectUrl = _settings.FallbackUnauthorizedRedirectUrl,
                    AutoRevokeOnResultExchange = _settings.AutoRevokeOnResultExchange,
                    RequireStrictLocalhostPort = _settings.RequireStrictLocalhostPort,
                    EnforceScopeAccess = _settings.EnforceScopeAccess,
                    ContextCookieExpiryDays = _settings.ContextCookieExpiryDays,
                    AllowedScopes = new List<string>(_settings.AllowedScopes ?? new List<string>()),
                    ProfileScopeAllowedActions = new List<string>(_settings.ProfileScopeAllowedActions ?? new List<string>()),
                    UsersScopeAllowedActions = new List<string>(_settings.UsersScopeAllowedActions ?? new List<string>())
                };
            }
        }

        public async Task<Result<RpapSettings>> UpdateSettingsAsync(RpapSettings newSettings)
        {
            if (newSettings == null)
            {
                return Result<RpapSettings>.Fail("Settings payload cannot be null.");
            }

            if (newSettings.MinTtlSeconds < 10) newSettings.MinTtlSeconds = 10;
            if (newSettings.MaxTtlSeconds > 604800) newSettings.MaxTtlSeconds = 604800; // max 7 days
            if (newSettings.MinTtlSeconds > newSettings.MaxTtlSeconds)
            {
                return Result<RpapSettings>.Fail("Minimum TTL cannot exceed Maximum TTL.");
            }

            if (newSettings.DefaultProfileTtlSeconds < newSettings.MinTtlSeconds)
                newSettings.DefaultProfileTtlSeconds = newSettings.MinTtlSeconds;
            if (newSettings.DefaultProfileTtlSeconds > newSettings.MaxTtlSeconds)
                newSettings.DefaultProfileTtlSeconds = newSettings.MaxTtlSeconds;

            if (newSettings.DefaultManagementTtlSeconds < newSettings.MinTtlSeconds)
                newSettings.DefaultManagementTtlSeconds = newSettings.MinTtlSeconds;
            if (newSettings.DefaultManagementTtlSeconds > newSettings.MaxTtlSeconds)
                newSettings.DefaultManagementTtlSeconds = newSettings.MaxTtlSeconds;

            if (string.IsNullOrWhiteSpace(newSettings.ProfileLandingUrl))
                newSettings.ProfileLandingUrl = "/Account/Profile";
            if (string.IsNullOrWhiteSpace(newSettings.UsersLandingUrl))
                newSettings.UsersLandingUrl = "/Home/Users";
            if (string.IsNullOrWhiteSpace(newSettings.FallbackUnauthorizedRedirectUrl))
                newSettings.FallbackUnauthorizedRedirectUrl = "/Account/Profile";

            if (newSettings.ContextCookieExpiryDays < 1) newSettings.ContextCookieExpiryDays = 1;
            if (newSettings.ContextCookieExpiryDays > 365) newSettings.ContextCookieExpiryDays = 365;

            newSettings.AllowedScopes ??= new List<string>();
            if (!newSettings.AllowedScopes.Any())
            {
                newSettings.AllowedScopes = new List<string> { ApplicationConstants.RpapConfig.Scopes.ProfileManage, ApplicationConstants.RpapConfig.Scopes.UsersManage, ApplicationConstants.RpapConfig.Scopes.UsersRolesManage };
            }

            newSettings.ProfileScopeAllowedActions ??= new List<string>();
            newSettings.UsersScopeAllowedActions ??= new List<string>();

            // Persist to Database
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dbEntity = await dbContext.RpapSettings.FirstOrDefaultAsync(s => s.TenantId == null);
                if (dbEntity == null)
                {
                    dbEntity = new RpapSetting
                    {
                        Id = Guid.NewGuid(),
                        TenantId = null
                    };
                    MapToEntity(newSettings, dbEntity);
                    await dbContext.RpapSettings.AddAsync(dbEntity);
                }
                else
                {
                    MapToEntity(newSettings, dbEntity);
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist RpapSettings to Database.");
            }

            lock (_lock)
            {
                _settings = newSettings;
                _isLoadedFromDb = true;
            }

            _logger.LogInformation("RPAP Protocol & SsoContextFilter settings saved to Database & memory cache successfully.");
            return Result<RpapSettings>.Success(GetSettings(), "Protocol settings saved successfully.");
        }

        public async Task<Result<RpapSettings>> ResetToDefaultsAsync()
        {
            var defaultSettings = new RpapSettings();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dbEntity = await dbContext.RpapSettings.FirstOrDefaultAsync(s => s.TenantId == null);
                if (dbEntity != null)
                {
                    MapToEntity(defaultSettings, dbEntity);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset RpapSettings in Database.");
            }

            lock (_lock)
            {
                _settings = defaultSettings;
                _isLoadedFromDb = true;
            }

            _logger.LogInformation("RPAP Protocol settings reset to factory defaults in Database.");
            return Result<RpapSettings>.Success(GetSettings(), "Protocol settings reset to defaults.");
        }

        public string ResolveLandingUrl(string? scope, string? requestedTargetUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(requestedTargetUrl))
                return requestedTargetUrl;

            var settings = GetSettings();
            var normalized = scope?.Trim().ToLowerInvariant();

            if (normalized == ApplicationConstants.RpapConfig.Scopes.ProfileManage)
            {
                return !string.IsNullOrWhiteSpace(settings.ProfileLandingUrl) ? settings.ProfileLandingUrl : "/Account/Profile";
            }

            return !string.IsNullOrWhiteSpace(settings.UsersLandingUrl) ? settings.UsersLandingUrl : "/Home/Users";
        }

        public int ResolveTtl(int? requestedTtlSeconds, string? scope)
        {
            var settings = GetSettings();
            int ttl;

            if (requestedTtlSeconds.HasValue && requestedTtlSeconds.Value > 0)
            {
                ttl = requestedTtlSeconds.Value;
            }
            else
            {
                var normalized = scope?.Trim().ToLowerInvariant();
                ttl = (normalized == ApplicationConstants.RpapConfig.Scopes.ProfileManage)
                    ? settings.DefaultProfileTtlSeconds
                    : settings.DefaultManagementTtlSeconds;
            }

            if (ttl < settings.MinTtlSeconds) ttl = settings.MinTtlSeconds;
            if (ttl > settings.MaxTtlSeconds) ttl = settings.MaxTtlSeconds;

            return ttl;
        }

        public bool IsScopeAllowed(string? scope)
        {
            if (string.IsNullOrWhiteSpace(scope)) return false;
            var settings = GetSettings();
            var normalized = scope.Trim().ToLowerInvariant();
            return settings.AllowedScopes != null && settings.AllowedScopes.Any(s => string.Equals(s.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsActionAllowed(string? scope, string? controllerName, string? actionName)
        {
            var settings = GetSettings();
            if (!settings.EnforceScopeAccess) return true; // Enforcement turned off

            if (string.IsNullOrEmpty(scope)) return true;

            var ctrl = controllerName?.Trim() ?? "";
            var act = actionName?.Trim() ?? "";

            // Account and Management controllers are always open for authenticated users
            if (string.Equals(ctrl, "Account", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ctrl, "Management", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var normalizedScope = scope.Trim().ToLowerInvariant();

            if (string.Equals(normalizedScope, ApplicationConstants.RpapConfig.Scopes.ProfileManage, StringComparison.OrdinalIgnoreCase))
            {
                var allowed = settings.ProfileScopeAllowedActions ?? new List<string>();
                return allowed.Any(a => string.Equals(a.Trim(), act, StringComparison.OrdinalIgnoreCase));
            }
            else if (string.Equals(normalizedScope, ApplicationConstants.RpapConfig.Scopes.UsersManage, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(normalizedScope, ApplicationConstants.RpapConfig.Scopes.UsersRolesManage, StringComparison.OrdinalIgnoreCase))
            {
                var allowed = settings.UsersScopeAllowedActions ?? new List<string>();
                return allowed.Any(a => string.Equals(a.Trim(), act, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        public string GetFallbackUnauthorizedRedirectUrl()
        {
            var settings = GetSettings();
            return !string.IsNullOrWhiteSpace(settings.FallbackUnauthorizedRedirectUrl)
                ? settings.FallbackUnauthorizedRedirectUrl
                : "/Account/Profile";
        }

        private static RpapSettings MapFromEntity(RpapSetting entity)
        {
            return new RpapSettings
            {
                DefaultProfileTtlSeconds = entity.DefaultProfileTtlSeconds,
                DefaultManagementTtlSeconds = entity.DefaultManagementTtlSeconds,
                MinTtlSeconds = entity.MinTtlSeconds,
                MaxTtlSeconds = entity.MaxTtlSeconds,
                ProfileLandingUrl = entity.ProfileLandingUrl,
                UsersLandingUrl = entity.UsersLandingUrl,
                FallbackUnauthorizedRedirectUrl = entity.FallbackUnauthorizedRedirectUrl,
                AutoRevokeOnResultExchange = entity.AutoRevokeOnResultExchange,
                RequireStrictLocalhostPort = entity.RequireStrictLocalhostPort,
                EnforceScopeAccess = entity.EnforceScopeAccess,
                ContextCookieExpiryDays = entity.ContextCookieExpiryDays,
                AllowedScopes = entity.AllowedScopes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>(),
                ProfileScopeAllowedActions = entity.ProfileScopeAllowedActions?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>(),
                UsersScopeAllowedActions = entity.UsersScopeAllowedActions?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? new List<string>()
            };
        }

        private static void MapToEntity(RpapSettings model, RpapSetting entity)
        {
            entity.DefaultProfileTtlSeconds = model.DefaultProfileTtlSeconds;
            entity.DefaultManagementTtlSeconds = model.DefaultManagementTtlSeconds;
            entity.MinTtlSeconds = model.MinTtlSeconds;
            entity.MaxTtlSeconds = model.MaxTtlSeconds;
            entity.ProfileLandingUrl = model.ProfileLandingUrl;
            entity.UsersLandingUrl = model.UsersLandingUrl;
            entity.FallbackUnauthorizedRedirectUrl = model.FallbackUnauthorizedRedirectUrl;
            entity.AutoRevokeOnResultExchange = model.AutoRevokeOnResultExchange;
            entity.RequireStrictLocalhostPort = model.RequireStrictLocalhostPort;
            entity.EnforceScopeAccess = model.EnforceScopeAccess;
            entity.ContextCookieExpiryDays = model.ContextCookieExpiryDays;
            entity.AllowedScopes = string.Join(",", model.AllowedScopes ?? new List<string>());
            entity.ProfileScopeAllowedActions = string.Join(",", model.ProfileScopeAllowedActions ?? new List<string>());
            entity.UsersScopeAllowedActions = string.Join(",", model.UsersScopeAllowedActions ?? new List<string>());
        }
    }
}
