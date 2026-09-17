namespace SSO.Domain.Contract
{
    public interface IAuditableEntity<TId> : IAuditableEntity, IEntity<TId>
    {
        //public TId TenantId { get; set; }
    }

    public interface IAuditableEntity : IEntity
    {
       
        string? CreatedBy { get; set; }

        DateTime? CreatedOn { get; set; }

        string? LastModifiedBy { get; set; }

        DateTime? LastModifiedOn { get; set; }
        string? IPAddress { get; set; }

        bool IsDeleted { get; set; }
    }
}
