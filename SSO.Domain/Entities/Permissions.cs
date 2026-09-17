using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Domain.Entities
{
    public class Permission : AuditableEntity<Guid>
    {
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;

        // Optional: App specific
        public Guid? ClientApplicationId { get; set; }
    }
}
