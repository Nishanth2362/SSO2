using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Responses.Features
{
    public class SubscriptionResponse
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public int MaxUsers { get; set; }
        public int MaxApps { get; set; }

        public bool AllowSeparateDb { get; set; }
        public Domain.Enums.SubscriptionBillingCycle BillingCycle { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public string? IPAddress { get; set; }
        public bool IsDeleted { get; set; }
    }
}
