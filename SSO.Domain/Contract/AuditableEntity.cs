using System.ComponentModel.DataAnnotations;

namespace SSO.Domain.Contract
{
    public abstract class AuditableEntity<TId> : IAuditableEntity<TId>
    {        
        public TId Id { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; } = null;
        public DateTime? LastModifiedOn { get; set; }
        public string? IPAddress { get; set; }
        public bool IsDeleted { get; set; }
        //public TId TenantId { get; set ; }
    }
}
