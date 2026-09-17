using Microsoft.AspNetCore.Identity;
using SSO.Domain.Contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity<Guid>
    {
        public Guid TenantId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get ; set ; }
        public DateTime? CreatedOn { get ; set ; }
        public string? LastModifiedBy { get ; set ; }
        public DateTime? LastModifiedOn { get ; set ; }
        public string? IPAddress { get ; set ; }
        public bool IsDeleted { get ; set ; }
    }
}
