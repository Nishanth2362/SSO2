using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Security;
using System.Text;

namespace SSO.Domain.Entities
{
    public class RolePermission : AuditableEntity<Guid>
    {
        public Guid RoleId { get; set; }
        [ForeignKey(nameof(RoleId))]
        public virtual ApplicationRole Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        [ForeignKey(nameof(PermissionId))]
        public virtual Permission Permission { get; set; } = null!;

        public Guid ApplicationClientId { get; set; }   
        [ForeignKey(nameof(ApplicationClientId))]
        public virtual ApplicationClient ApplicationClient { get; set; } = null!;
    }
}
