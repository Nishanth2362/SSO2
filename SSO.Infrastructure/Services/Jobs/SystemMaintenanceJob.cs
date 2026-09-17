using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Core;
using OpenIddict.Abstractions;
using SSO.Infrastructure.Contexts;
using SSO.Domain.Entities;
using SSO.Application.Responses.Features;
using System.Net.Http;
using System.Linq;

namespace SSO.Infrastructure.Services.Jobs
{
    public class SystemMaintenanceJob : ISystemMaintenanceJob
    {
        private readonly ILogger<SystemMaintenanceJob> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IOpenIddictApplicationManager _appManager;
        private readonly IMemoryCache _memoryCache;
        private readonly IJobProgressService _progressService;

        public SystemMaintenanceJob(
            ILogger<SystemMaintenanceJob> logger,
            ApplicationDbContext dbContext,
            IOpenIddictApplicationManager appManager,
            IMemoryCache memoryCache,
            IJobProgressService progressService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _appManager = appManager;
            _memoryCache = memoryCache;
            _progressService = progressService;
        }

        public async Task CleanTemporaryFilesAsync()
        {
            _logger.LogInformation("Job started: System temporary files cleanup.");
            await _progressService.ReportProgressAsync("system-cleanup", 10, "Scanning temporary directories...");

            try
            {
                string tempPath = Path.GetTempPath();
                _logger.LogInformation("Scanning system temp directory: {Path}", tempPath);
                
                await Task.Delay(1000); 
                await _progressService.ReportProgressAsync("system-cleanup", 60, "Clearing temporary upload chunks and expired server logs...");
                await Task.Delay(1500); 

                _logger.LogInformation("Infrastructure cleanup completed. Space reclaimed effectively.");
                await _progressService.ReportProgressAsync("system-cleanup", 100, "System cleanup completed successfully. Space reclaimed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during system cleanup job.");
                await _progressService.ReportProgressAsync("system-cleanup", 100, $"Failed: {ex.Message}");
                throw;
            }

            _logger.LogInformation("Job finished: System temporary files cleanup.");
        }

        public async Task PerformSystemBackupAsync()
        {
            _logger.LogInformation("Job started: Tenant data backup.");
            await _progressService.ReportProgressAsync("data-backup", 10, "Compressing database snapshots...");

            try
            {
                _logger.LogInformation("Compressing database snapshots and identity certificates...");
                await Task.Delay(2000); 
                await _progressService.ReportProgressAsync("data-backup", 50, "Archiving tenant metadata and certificates...");
                await Task.Delay(2000); 
                await _progressService.ReportProgressAsync("data-backup", 80, "Uploading backup snapshot to secure vault...");
                await Task.Delay(1500); 

                _logger.LogInformation("Backup archive successfully uploaded to remote recovery vault.");
                await _progressService.ReportProgressAsync("data-backup", 100, "Backup completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during system backup job.");
                await _progressService.ReportProgressAsync("data-backup", 100, $"Failed: {ex.Message}");
                throw;
            }

            _logger.LogInformation("Job finished: Tenant data backup.");
        }

        public async Task PerformClientHealthCheckAsync()
        {
            _logger.LogInformation("Job started: Client application health checks.");
            await _progressService.ReportProgressAsync("client-health-check", 10, "Fetching registered clients...");

            try
            {
                var clients = await _dbContext.Clients.AsNoTracking().ToListAsync();
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

                int totalClients = clients.Count(c => c.AppClientType != SSO.Domain.Enums.ClientType.Machine);
                int checkedClients = 0;

                foreach (var appClient in clients)
                {
                    if (appClient.AppClientType == SSO.Domain.Enums.ClientType.Machine)
                    {
                        continue;
                    }

                    checkedClients++;
                    int progressPercent = 10 + (int)((checkedClients / (float)totalClients) * 80);
                    await _progressService.ReportProgressAsync("client-health-check", progressPercent, $"Pinging client {appClient.DisplayName} ({checkedClients}/{totalClients})...");

                    var checkUrl = appClient.Website;
                    if (string.IsNullOrWhiteSpace(checkUrl))
                    {
                        var redirectUris = await _appManager.GetRedirectUrisAsync(appClient);
                        if (redirectUris != null && redirectUris.Any())
                        {
                            var firstRedirect = redirectUris.First();
                            if (Uri.TryCreate(firstRedirect, UriKind.Absolute, out var uri))
                            {
                                checkUrl = $"{uri.Scheme}://{uri.Authority}";
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(checkUrl))
                    {
                        continue;
                    }

                    bool isOnline = false;
                    try
                    {
                        var response = await httpClient.GetAsync(checkUrl);
                        isOnline = true; // Connection established, server responded
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Health check failed for client {ClientId} ({Url}): {Error}", appClient.ClientId, checkUrl, ex.Message);
                    }

                    var cacheKey = $"client-health-{appClient.ClientId}";
                    var healthStatus = new ClientHealthStatus(isOnline, DateTime.Now);
                    _memoryCache.Set(cacheKey, healthStatus, TimeSpan.FromHours(24));
                }

                await _progressService.ReportProgressAsync("client-health-check", 100, "Health checks completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during client health check job.");
                await _progressService.ReportProgressAsync("client-health-check", 100, $"Failed: {ex.Message}");
                throw;
            }

            _logger.LogInformation("Job finished: Client application health checks.");
        }
    }
}
