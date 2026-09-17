using System;

namespace SSO.Application.Responses.Billing
{
    public class InvoiceResponse
    {
        public Guid Id { get; set; }
        public string TenantName { get; set; }
        public string SubscriptionName { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
    }
}
