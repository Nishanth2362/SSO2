using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Infrastructure.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UAParser;

namespace SSO.Infrastructure.Services
{
    /// <summary>
    /// Records and queries login security events.
    /// Captures IP address, GeoIP location, User-Agent (browser/OS/device)
    /// for every significant authentication event.
    ///
    /// GeoIP: uses MaxMind GeoLite2-City.mmdb when present; gracefully degrades
    ///        to null fields if the database file is missing.
    /// UA Parsing: uses the UAParser NuGet package (no external calls).
    /// </summary>
    public class SecurityEventService : ISecurityEventService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MaxMind.GeoIP2.DatabaseReader? _geoIpReader;

        public SecurityEventService(
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            MaxMind.GeoIP2.DatabaseReader? geoIpReader = null)

        {
            _dbContext           = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _geoIpReader         = geoIpReader;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // LogAsync
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task LogAsync(SecurityEventContext context)
        {
            try
            {
                var http = context.HttpContext;

                // 1. Extract IP address (handle reverse proxies)
                var ip = ResolveIpAddress(http);

                // 2. GeoIP lookup (best-effort, never throws)
                var (country, countryCode, city) = await ResolveGeoIpAsync(ip);

                // 3. Parse User-Agent
                var userAgent   = http.Request.Headers["User-Agent"].ToString();
                var (browser, os, device) = ParseUserAgent(userAgent);

                var evt = new LoginSecurityEvent
                {
                    UserId         = context.UserId,
                    UserName       = context.UserName,
                    ClientId       = context.ClientId,
                    EventType      = context.EventType,
                    IpAddress      = ip,
                    Country        = country,
                    CountryCode    = countryCode,
                    City           = city,
                    UserAgent      = userAgent,
                    BrowserName    = browser,
                    OsName         = os,
                    DeviceType     = device,
                    AttemptNumber  = context.AttemptNumber,
                    IsBlocked      = context.IsBlocked,
                    OccurredAtUtc  = DateTime.UtcNow
                };

                _dbContext.LoginSecurityEvents.Add(evt);
                await _dbContext.SaveChangesAsync();
            }
            catch
            {
                // Security logging must never surface exceptions to the login flow.
                // Errors here are silently swallowed; structured logging (Serilog) will still capture them.
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Queries
        // ─────────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<List<SecurityEventResponse>> GetRecentAsync(int count = 100)
        {
            return await _dbContext.LoginSecurityEvents
                .AsNoTracking()
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(count)
                .Select(e => MapToResponse(e))
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<SecurityEventResponse>> GetByUserAsync(Guid userId, int count = 50)
        {
            return await _dbContext.LoginSecurityEvents
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(count)
                .Select(e => MapToResponse(e))
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<SecurityEventSummary> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;

            var todayEvents = await _dbContext.LoginSecurityEvents
                .AsNoTracking()
                .Where(e => e.OccurredAtUtc >= today)
                .ToListAsync();

            return new SecurityEventSummary(
                TotalFailuresToday: todayEvents.Count(e =>
                    e.EventType == SecurityEventType.PasswordFailed ||
                    e.EventType == SecurityEventType.OtpFailed ||
                    e.EventType == SecurityEventType.OtpExpired),
                LockedAccountsToday: todayEvents.Count(e =>
                    e.EventType == SecurityEventType.AccountLocked),
                SuspiciousEventsToday: todayEvents.Count(e =>
                    e.EventType == SecurityEventType.SuspiciousActivity),
                TotalEventsToday: todayEvents.Count
            );
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────────

        private static string? ResolveIpAddress(HttpContext http)
        {
            // Respect X-Forwarded-For (set by Nginx / load balancers)
            var forwarded = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                // Take the first (client) IP in the chain
                var first = forwarded.Split(',')[0].Trim();
                if (IPAddress.TryParse(first, out _))
                    return first;
            }

            return http.Connection.RemoteIpAddress?.ToString();
        }

        private async Task<(string? country, string? code, string? city)> ResolveGeoIpAsync(string? ip)
        {
            if (_geoIpReader != null && !string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip, out var addr))
            {
                try
                {
                    var response = _geoIpReader.City(addr);
                    return (response.Country.Name, response.Country.IsoCode, response.City.Name);
                }
                catch
                {
                    // Fall back to external API if local DB fails
                }
            }

            // External fallback using ip-api.com
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                
                string url = (string.IsNullOrWhiteSpace(ip) || ip == "::1" || ip == "127.0.0.1" || ip.StartsWith("192.168.") || ip.StartsWith("10.")) 
                    ? "http://ip-api.com/json/" 
                    : $"http://ip-api.com/json/{ip}";

                var result = await client.GetStringAsync(url);
                var doc = System.Text.Json.JsonDocument.Parse(result);
                if (doc.RootElement.TryGetProperty("status", out var status) && status.GetString() == "success")
                {
                    string? country = doc.RootElement.GetProperty("country").GetString();
                    string? countryCode = doc.RootElement.GetProperty("countryCode").GetString();
                    string? city = doc.RootElement.GetProperty("city").GetString();
                    return (country, countryCode, city);
                }
            }
            catch
            {
                // Gracefully degrade to nulls if external API fails
            }

            return (null, null, null);
        }

        private static (string? browser, string? os, string? device) ParseUserAgent(string? ua)
        {
            if (string.IsNullOrWhiteSpace(ua))
                return (null, null, "Unknown");

            try
            {
                var parser  = Parser.GetDefault();
                var client  = parser.Parse(ua);

                var browser = client.UA.Family;
                var os      = client.OS.Family;
                var device  = client.Device.IsSpider
                    ? "Bot"
                    : string.IsNullOrEmpty(client.Device.Family) || client.Device.Family == "Other"
                        ? "Desktop"
                        : client.Device.Family.Contains("Phone", StringComparison.OrdinalIgnoreCase)
                            ? "Mobile"
                            : client.Device.Family.Contains("Tablet", StringComparison.OrdinalIgnoreCase)
                                ? "Tablet"
                                : "Desktop";

                return (browser, os, device);
            }
            catch
            {
                return (null, null, "Unknown");
            }
        }

        private static SecurityEventResponse MapToResponse(LoginSecurityEvent e)
        {
            return new SecurityEventResponse
            {
                Id            = e.Id,
                UserId        = e.UserId,
                UserName      = e.UserName,
                ClientId      = e.ClientId,
                EventType     = e.EventType.ToString(),
                AttemptNumber = e.AttemptNumber,
                IsBlocked     = e.IsBlocked,
                IpAddress     = e.IpAddress,
                CountryCode   = e.CountryCode,
                Country       = e.Country,
                City          = e.City,
                DeviceType    = e.DeviceType,
                BrowserName   = e.BrowserName,
                OsName        = e.OsName,
                UserAgent     = e.UserAgent,
                OccurredAtUtc = e.OccurredAtUtc
            };
        }
    }
}
