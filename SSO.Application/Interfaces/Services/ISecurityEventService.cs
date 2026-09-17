using Microsoft.AspNetCore.Http;
using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    /// <summary>
    /// Persists and queries authentication security events for monitoring,
    /// alerting, and the SSO Security Management dashboard.
    /// </summary>
    public interface ISecurityEventService
    {
        /// <summary>
        /// Logs a security event asynchronously. Should be called fire-and-forget
        /// to avoid any latency impact on the login request path.
        /// </summary>
        Task LogAsync(SecurityEventContext context);

        /// <summary>Returns the <paramref name="count"/> most recent events across all users.</summary>
        Task<List<SecurityEventResponse>> GetRecentAsync(int count = 100);

        /// <summary>Returns security events for a specific user, newest first.</summary>
        Task<List<SecurityEventResponse>> GetByUserAsync(Guid userId, int count = 50);

        /// <summary>Returns aggregated summary statistics for the dashboard stat cards.</summary>
        Task<SecurityEventSummary> GetSummaryAsync();
    }

    /// <summary>Context passed to <see cref="ISecurityEventService.LogAsync"/>.</summary>
    public record SecurityEventContext(
        HttpContext HttpContext,
        Guid? UserId,
        string? UserName,
        string ClientId,
        SecurityEventType EventType,
        int AttemptNumber = 1,
        bool IsBlocked = false
    );

    /// <summary>Aggregated statistics for the Security Management dashboard.</summary>
    public record SecurityEventSummary(
        int TotalFailuresToday,
        int LockedAccountsToday,
        int SuspiciousEventsToday,
        int TotalEventsToday
    );
}
