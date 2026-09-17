using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Domain.Entities
{
    public class Subscriptions : AuditableEntity<Guid>
    {
        public string Name { get; set; }

        public int MaxUsers { get; set; }
        public int MaxApps { get; set; }

        public bool AllowSeparateDb { get; set; }
        
        public Enums.SubscriptionBillingCycle BillingCycle { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "INR";
        
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
