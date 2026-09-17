using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Domain.Entities
{
    public class ApplicationClientScope : AuditableEntity<Guid>
    {
        public Guid ClientId { get; set; }
        public ApplicationClient Client { get; set; }

        public Guid ScopeId { get; set; }
        public ApplicationScope Scope { get; set; }
    }
}
