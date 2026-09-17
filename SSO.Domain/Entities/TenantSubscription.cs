using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SSO.Domain.Entities
{
    public class TenantSubscription : AuditableEntity<Guid>
    {
        public Guid TenantId { get; set; }
        public Guid SubscriptionId { get; set; }

        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }

        public bool IsActive { get; set; }
        [ForeignKey(nameof(TenantId))]
        public virtual Tenants Tenants { get; set; } = null!;
        [ForeignKey(nameof(SubscriptionId))]
        public virtual Subscriptions Subscriptions { get; set; } = null!;
    }
}
