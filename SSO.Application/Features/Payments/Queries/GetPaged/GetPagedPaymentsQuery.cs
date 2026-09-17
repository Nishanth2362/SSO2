using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.Billing;
using SSO.Application.Responses.DataTable;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Payments.Queries.GetPaged
{
    public class GetPagedPaymentsQuery : DataTableRequest, IRequest<DataTableResponse<PaymentResponse>>
    {
        public Guid? InvoiceId { get; set; }
        public GetPagedPaymentsQuery(DataTableRequest request, Guid? invoiceId = null)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
            this.SearchColumn = request.SearchColumn;
            this.Filters = request.Filters;
            this.StartDate = request.StartDate;
            this.EndDate = request.EndDate;
            this.SelectedIds = request.SelectedIds;
            this.InvoiceId = invoiceId;
        }
    }

    internal class GetPagedPaymentsQueryHandler : IRequestHandler<GetPagedPaymentsQuery, DataTableResponse<PaymentResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedPaymentsQueryHandler> _logger;

        public GetPagedPaymentsQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedPaymentsQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DataTableResponse<PaymentResponse>> Handle(GetPagedPaymentsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.Payment>().Entities
                    .Include(x => x.Invoice)
                        .ThenInclude(i => i.TenantSubscription)
                            .ThenInclude(ts => ts.Tenants)
                    .AsNoTracking();

                if (request.InvoiceId.HasValue)
                {
                    query = query.Where(x => x.InvoiceId == request.InvoiceId.Value);
                }

                return await _dataTableService.BuildAsync(
                    query,
                    request,
                    e => new PaymentResponse
                    {
                        Id = e.Id,
                        InvoiceId = e.InvoiceId,
                        InvoiceNumber = e.Invoice.InvoiceNumber,
                        TenantName = e.Invoice.TenantSubscription.Tenants.Name,
                        TransactionId = e.TransactionId,
                        PaymentDate = e.PaymentDate,
                        Amount = e.Amount,
                        Method = e.Method.ToString(),
                        Status = e.Status.ToString(),
                        Currency = e.Invoice.Currency
                    },
                    e => true,
                    new List<string> { "TransactionId", "PaymentDate", "Amount" },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged payments");
                return new DataTableResponse<PaymentResponse>
                {
                    Data = new List<PaymentResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
