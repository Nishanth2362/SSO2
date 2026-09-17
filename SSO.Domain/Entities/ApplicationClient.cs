using OpenIddict.EntityFrameworkCore.Models;
using SSO.Domain.Contract;
using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Domain.Entities
{
    public class ApplicationClient
    : OpenIddictEntityFrameworkCoreApplication<Guid, ApplicationAuthorization, ApplicationToken>, IAuditableEntity<Guid>
    {
        // 🔥 YOUR BUSINESS FIELDS
        public ICollection<TenantClient> TenantClients { get; set; } = new List<TenantClient>();
        public string? Audience { get; set; }
        public bool IsActive { get; set; }
        public bool RequireLicense { get; set; }
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public ClientType AppClientType { get; set; }
        // navigation
        public ICollection<ApplicationClientScope> ClientScopes { get; set; }
            = new List<ApplicationClientScope>();
        public string? CreatedBy { get ; set ; }
        public DateTime? CreatedOn { get ; set ; }
        public string? LastModifiedBy { get ; set ; }
        public DateTime? LastModifiedOn { get ; set ; }
        public string? IPAddress { get ; set ; }
        public bool IsDeleted { get ; set ; }
    }
}
