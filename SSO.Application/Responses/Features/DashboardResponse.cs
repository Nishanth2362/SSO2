using System;
using System.Collections.Generic;

namespace SSO.Application.Responses.Features
{
    public class DashboardResponse
    {
        public int TotalUsers { get; set; }
        public int TotalTenants { get; set; }
        public long TotalClients { get; set; }
        public int ActiveSubscriptions { get; set; }
        public string IssuerUri { get; set; }
        public string CertificateType { get; set; }
        public string EncryptionAlgorithm { get; set; }
        public string DatabaseProvider { get; set; }
        
        public List<RecentActivityResponse> RecentActivities { get; set; } = new List<RecentActivityResponse>();
    }

    public class RecentActivityResponse
    {
        public string Timestamp { get; set; }
        public string Email { get; set; }
        public string ClientApp { get; set; }
        public string GrantType { get; set; }
        public string Status { get; set; }
    }
}
