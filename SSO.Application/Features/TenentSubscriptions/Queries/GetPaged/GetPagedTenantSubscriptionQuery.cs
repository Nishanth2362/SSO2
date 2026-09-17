using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.TenentSubscriptions.Queries.GetPaged
{
    public class GetPagedTenantSubscriptionQuery : DataTableRequest, IRequest<DataTableResponse<TenantSubscriptionResponse>>
    {
        public Guid TenantId { get; set; } = Guid.Empty;
        public GetPagedTenantSubscriptionQuery(DataTableRequest request, Guid tenantId = default)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
            this.TenantId = tenantId;
        }
    }

    internal class GetPagedTenantSubscriptionQueryHandler : IRequestHandler<GetPagedTenantSubscriptionQuery, DataTableResponse<TenantSubscriptionResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedTenantSubscriptionQueryHandler> _logger;

        public GetPagedTenantSubscriptionQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedTenantSubscriptionQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DataTableResponse<TenantSubscriptionResponse>> Handle(GetPagedTenantSubscriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.TenantSubscription>().Entities
                    .Include(x => x.Tenants)
                    .Include(x => x.Subscriptions)
                    .AsNoTracking();

                if (request.TenantId != Guid.Empty)
                {
                    query = query.Where(x => x.TenantId == request.TenantId);
                }

                return await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new TenantSubscriptionResponse
                            {
                                Id = e.Id,
                                TenantId = e.TenantId,
                                TenantName = e.Tenants != null ? e.Tenants.Name : string.Empty,
                                SubscriptionId = e.SubscriptionId,
                                SubscriptionName = e.Subscriptions != null ? e.Subscriptions.Name : string.Empty,
                                StartDateUtc = e.StartDateUtc,
                                EndDateUtc = e.EndDateUtc,
                                IsActive = e.IsActive
                            },
                            e => true,
                            new List<string>
                            {
                                "Tenants.Name",
                                "Subscriptions.Name"
                            },
                            cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting paged tenant subscriptions.");
                return new DataTableResponse<TenantSubscriptionResponse>
                {
                    Data = new List<TenantSubscriptionResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
