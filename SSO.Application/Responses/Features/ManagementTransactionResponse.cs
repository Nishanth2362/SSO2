using SSO.Domain.Enums;
using System;

namespace SSO.Application.Responses.Features
{
    public class ManagementTransactionResponse
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string? ClientDisplayName { get; set; }
        public Guid TenantId { get; set; }
        public string? TenantName { get; set; }
        public Guid UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string Scope { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string? State { get; set; }
        public DateTime ExpiresOn { get; set; }
        public bool IsConsumed { get; set; }
        public DateTime? ConsumedOn { get; set; }
        public string? ConsumedIpAddress { get; set; }
        public string? ConsumedUserAgent { get; set; }
        public bool HasResultCode { get; set; }
        public bool IsResultCodeConsumed { get; set; }
        public DateTime? ResultCodeConsumedOn { get; set; }
        public ManagementTransactionStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public string? RevokedReason { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string? RevokedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public string? ChangedFieldsJson { get; set; }
        public System.Collections.Generic.List<string> Changes { get; set; } = new();
        public bool HasChanges => Changes.Count > 0;
    }

    public class ManagementSummaryResponse
    {
        public int TotalTransactions { get; set; }
        public int ActiveSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int ExpiredTransactions { get; set; }
        public int RevokedSessions { get; set; }
        public int ProfileScopeCount { get; set; }
        public int UsersScopeCount { get; set; }
        public int UsersRolesScopeCount { get; set; }
    }
}
