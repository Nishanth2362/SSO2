using System;

namespace SSO.Application.Responses.Features
{
    public class TenantSubscriptionResponse
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
        public Guid SubscriptionId { get; set; }
        public string SubscriptionName { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public bool IsActive { get; set; }
    }
}
