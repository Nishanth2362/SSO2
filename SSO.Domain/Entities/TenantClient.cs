using System;
using System.ComponentModel.DataAnnotations.Schema;
using SSO.Domain.Contract;

namespace SSO.Domain.Entities
{
    public class TenantClient:AuditableEntity<Guid>
    {
        public Guid TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenants Tenant { get; set; }

        public Guid ApplicationClientId { get; set; }
        [ForeignKey(nameof(ApplicationClientId))]
        public ApplicationClient ApplicationClient { get; set; }
    }
}
