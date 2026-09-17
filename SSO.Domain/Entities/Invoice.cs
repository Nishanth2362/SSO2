using SSO.Domain.Contract;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SSO.Domain.Entities
{
    public class Invoice : AuditableEntity<Guid>
    {
        public Guid TenantSubscriptionId { get; set; }
        public virtual TenantSubscription TenantSubscription { get; set; }

        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        
        [Precision(18, 2)]
        public decimal TaxAmount { get; set; }
        
        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "INR";

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public enum InvoiceStatus
    {
        Pending,
        Paid,
        Overdue,
        Cancelled
    }
}
