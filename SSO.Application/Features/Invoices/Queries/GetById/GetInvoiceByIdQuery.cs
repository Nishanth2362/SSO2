using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Billing;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Invoices.Queries.GetById
{
    public class GetInvoiceByIdQuery : IRequest<Result<InvoiceDetailResponse>>
    {
        public Guid Id { get; set; }
        public GetInvoiceByIdQuery(Guid id) => Id = id;
    }

    public class InvoiceDetailResponse : InvoiceResponse
    {
        public string BillingAddress { get; set; }
        public string TenantEmail { get; set; }
        public string TenantPhone { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        // We can add items here if needed
    }

    internal class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetailResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        public GetInvoiceByIdQueryHandler(IUnitOfWork<Guid> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<InvoiceDetailResponse>> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
        {
            var invoice = await _unitOfWork.Repository<Domain.Entities.Invoice>().Entities
                .Include(x => x.TenantSubscription)
                    .ThenInclude(ts => ts.Tenants)
                .Include(x => x.TenantSubscription)
                    .ThenInclude(ts => ts.Subscriptions)
                .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

            if (invoice == null) return await Result<InvoiceDetailResponse>.FailAsync("Invoice not found.");

            var response = new InvoiceDetailResponse
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Amount = invoice.Amount,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status.ToString(),
                TenantName = invoice.TenantSubscription.Tenants.Name,
                BillingAddress = invoice.TenantSubscription.Tenants.BillingAddress,
                TenantEmail = invoice.TenantSubscription.Tenants.Email,
                TenantPhone = invoice.TenantSubscription.Tenants.Phone,
                SubscriptionName = invoice.TenantSubscription.Subscriptions.Name,
                Currency = invoice.TenantSubscription.Tenants.Currency
            };

            return await Result<InvoiceDetailResponse>.SuccessAsync(response);
        }
    }
}
