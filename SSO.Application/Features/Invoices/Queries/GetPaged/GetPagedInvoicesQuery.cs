using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Billing;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Invoices.Queries.GetPaged
{
    public class GetPagedInvoicesQuery : DataTableRequest, IRequest<DataTableResponse<InvoiceResponse>>
    {
        public Guid? TenantId { get; set; }
        public GetPagedInvoicesQuery(DataTableRequest request, Guid? tenantId = null)
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
            this.TenantId = tenantId;
        }
    }

    internal class GetPagedInvoicesQueryHandler : IRequestHandler<GetPagedInvoicesQuery, DataTableResponse<InvoiceResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedInvoicesQueryHandler> _logger;

        public GetPagedInvoicesQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedInvoicesQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DataTableResponse<InvoiceResponse>> Handle(GetPagedInvoicesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.Invoice>().Entities
                    .Include(x => x.TenantSubscription)
                        .ThenInclude(ts => ts.Tenants)
                    .Include(x => x.TenantSubscription)
                        .ThenInclude(ts => ts.Subscriptions)
                    .AsNoTracking();

                if (request.TenantId.HasValue)
                {
                    query = query.Where(x => x.TenantSubscription.TenantId == request.TenantId.Value);
                }

                return await _dataTableService.BuildAsync(
                    query,
                    request,
                    e => new InvoiceResponse
                    {
                        Id = e.Id,
                        TenantName = e.TenantSubscription.Tenants.Name,
                        SubscriptionName = e.TenantSubscription.Subscriptions.Name,
                        InvoiceNumber = e.InvoiceNumber,
                        InvoiceDate = e.InvoiceDate,
                        DueDate = e.DueDate,
                        TotalAmount = e.TotalAmount,
                        Currency = e.TenantSubscription.Tenants.Currency,
                        Status = e.Status.ToString()
                    },
                    e => true,
                    new List<string> { "InvoiceNumber", "InvoiceDate", "DueDate", "TotalAmount" },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged invoices");
                return new DataTableResponse<InvoiceResponse>
                {
                    Data = new List<InvoiceResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
