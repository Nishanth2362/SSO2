using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Services;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using System;
using System.Threading.Tasks;

namespace SSO.Infrastructure.Services
{
    public class TokenLifetimeSettingsService : ITokenLifetimeSettingsService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<TokenLifetimeSettingsService> _logger;

        private const string CacheKeyPrefix = "TokenLifetimeSettings_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        public TokenLifetimeSettingsService(
            ApplicationDbContext dbContext,
            IConfiguration configuration,
            IMemoryCache memoryCache,
            ILogger<TokenLifetimeSettingsService> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<TokenLifetimeSettings> GetActiveSettingsAsync(Guid? tenantId = null)
        {
            string cacheKey = $"{CacheKeyPrefix}{tenantId?.ToString() ?? "Global"}";

            if (_memoryCache.TryGetValue(cacheKey, out TokenLifetimeSettings? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var dbSetting = await _dbContext.TokenLifetimeSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId);

                if (dbSetting != null)
                {
                    var settings = new TokenLifetimeSettings
                    {
                        AccessTokenLifetimeMinutes = dbSetting.AccessTokenLifetimeMinutes > 0 ? dbSetting.AccessTokenLifetimeMinutes : GetConfigFallback("AccessTokenLifetimeMinutes", 60),
                        RefreshTokenLifetimeDays = dbSetting.RefreshTokenLifetimeDays > 0 ? dbSetting.RefreshTokenLifetimeDays : GetConfigFallback("RefreshTokenLifetimeDays", 14),
                        AuthorizationCodeLifetimeMinutes = dbSetting.AuthorizationCodeLifetimeMinutes > 0 ? dbSetting.AuthorizationCodeLifetimeMinutes : GetConfigFallback("AuthorizationCodeLifetimeMinutes", 5),
                        TenantId = dbSetting.TenantId
                    };

                    _memoryCache.Set(cacheKey, settings, CacheDuration);
                    return settings;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load TokenLifetimeSettings from DB. Falling back to appsettings.json.");
            }

            // Fallback to appsettings.json
            var fallback = new TokenLifetimeSettings
            {
                AccessTokenLifetimeMinutes = GetConfigFallback("AccessTokenLifetimeMinutes", 60),
                RefreshTokenLifetimeDays = GetConfigFallback("RefreshTokenLifetimeDays", 14),
                AuthorizationCodeLifetimeMinutes = GetConfigFallback("AuthorizationCodeLifetimeMinutes", 5),
                TenantId = tenantId
            };

            _memoryCache.Set(cacheKey, fallback, CacheDuration);
            return fallback;
        }

        public async Task<Result<TokenLifetimeSettings>> UpdateSettingsAsync(TokenLifetimeSettings newSettings)
        {
            if (newSettings == null)
                return await Result<TokenLifetimeSettings>.FailAsync("Invalid settings payload.");

            // Validations
            if (newSettings.AccessTokenLifetimeMinutes < 1 || newSettings.AccessTokenLifetimeMinutes > 1440)
                return await Result<TokenLifetimeSettings>.FailAsync("Access Token Lifetime must be between 1 minute and 1440 minutes (24 hours).");

            if (newSettings.RefreshTokenLifetimeDays < 1 || newSettings.RefreshTokenLifetimeDays > 365)
                return await Result<TokenLifetimeSettings>.FailAsync("Refresh Token Lifetime must be between 1 day and 365 days.");

            if (newSettings.AuthorizationCodeLifetimeMinutes < 1 || newSettings.AuthorizationCodeLifetimeMinutes > 60)
                return await Result<TokenLifetimeSettings>.FailAsync("Authorization Code Lifetime must be between 1 minute and 60 minutes.");

            try
            {
                var existing = await _dbContext.TokenLifetimeSettings
                    .FirstOrDefaultAsync(x => x.TenantId == newSettings.TenantId);

                if (existing == null)
                {
                    existing = new TokenLifetimeSetting
                    {
                        Id = Guid.NewGuid(),
                        AccessTokenLifetimeMinutes = newSettings.AccessTokenLifetimeMinutes,
                        RefreshTokenLifetimeDays = newSettings.RefreshTokenLifetimeDays,
                        AuthorizationCodeLifetimeMinutes = newSettings.AuthorizationCodeLifetimeMinutes,
                        TenantId = newSettings.TenantId
                    };
                    await _dbContext.TokenLifetimeSettings.AddAsync(existing);
                }
                else
                {
                    existing.AccessTokenLifetimeMinutes = newSettings.AccessTokenLifetimeMinutes;
                    existing.RefreshTokenLifetimeDays = newSettings.RefreshTokenLifetimeDays;
                    existing.AuthorizationCodeLifetimeMinutes = newSettings.AuthorizationCodeLifetimeMinutes;
                }

                await _dbContext.SaveChangesAsync();

                // Invalidate Cache
                string cacheKey = $"{CacheKeyPrefix}{newSettings.TenantId?.ToString() ?? "Global"}";
                _memoryCache.Remove(cacheKey);

                var updated = await GetActiveSettingsAsync(newSettings.TenantId);
                return await Result<TokenLifetimeSettings>.SuccessAsync(updated, "Token Lifetime Settings updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update TokenLifetimeSettings.");
                return await Result<TokenLifetimeSettings>.FailAsync($"Failed to update settings: {ex.Message}");
            }
        }

        public async Task<Result<TokenLifetimeSettings>> ResetToDefaultsAsync(Guid? tenantId = null)
        {
            try
            {
                var existing = await _dbContext.TokenLifetimeSettings
                    .FirstOrDefaultAsync(x => x.TenantId == tenantId);

                if (existing != null)
                {
                    _dbContext.TokenLifetimeSettings.Remove(existing);
                    await _dbContext.SaveChangesAsync();
                }

                string cacheKey = $"{CacheKeyPrefix}{tenantId?.ToString() ?? "Global"}";
                _memoryCache.Remove(cacheKey);

                var defaults = await GetActiveSettingsAsync(tenantId);
                return await Result<TokenLifetimeSettings>.SuccessAsync(defaults, "Token Lifetime Settings reset to system defaults.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset TokenLifetimeSettings.");
                return await Result<TokenLifetimeSettings>.FailAsync($"Failed to reset settings: {ex.Message}");
            }
        }

        private int GetConfigFallback(string key, int defaultValue)
        {
            var configVal = _configuration.GetSection("TokenSettings").GetValue<int?>(key);
            return configVal.HasValue && configVal.Value > 0 ? configVal.Value : defaultValue;
        }
    }
}
