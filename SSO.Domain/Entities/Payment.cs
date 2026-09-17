using SSO.Domain.Contract;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSO.Domain.Entities
{
    public class Payment : AuditableEntity<Guid>
    {
        public Guid InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; }

        public string TransactionId { get; set; }
        public DateTime PaymentDate { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    }

    public enum PaymentMethod
    {
        CreditCard,
        PayPal,
        BankTransfer,
        Cash,
        Check,
        Other
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
}
