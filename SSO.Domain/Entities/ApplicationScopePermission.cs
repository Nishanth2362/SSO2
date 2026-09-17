using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SSO.Domain.Entities
{
    public class ApplicationScopePermission : AuditableEntity<Guid>
    {
        public Guid ScopeId { get; set; }
        [ForeignKey(nameof(ScopeId))]
        public ApplicationScope Scope { get; set; }

        public Guid PermissionId { get; set; }
        [ForeignKey(nameof(PermissionId))]
        public Permission Permission { get; set; }
    }
}
