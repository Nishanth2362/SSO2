using Microsoft.AspNetCore.Identity;
using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SSO.Domain.Entities
{
    public class ApplicationRole : IdentityRole<Guid>, IAuditableEntity<Guid>
    {
        public string Description { get; set; } 
        public Guid TenantId { get; set; }
        public bool IsSystemRole { get; set; }
        public string? CreatedBy { get ; set ; }
        public DateTime? CreatedOn { get ; set ; }
        public string? LastModifiedBy { get ; set ; }
        public DateTime? LastModifiedOn { get ; set ; }
        public string? IPAddress { get ; set ; }
        public bool IsDeleted { get ; set ; }
        [ForeignKey(nameof(TenantId))]
        public virtual Tenants Tenant { get; set; }
    }

    public class ApplicationUserRole : IdentityUserRole<Guid>
    {
        public Guid TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public virtual Tenants Tenant { get; set; }       
    }

    public class ApplicationUserClaim : IdentityUserClaim<Guid> { }

    public class ApplicationUserLogin : IdentityUserLogin<Guid> { }

    public class ApplicationUserToken : IdentityUserToken<Guid> { }

    public class ApplicationRoleClaim : IdentityRoleClaim<Guid> { }
}
