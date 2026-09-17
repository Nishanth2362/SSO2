using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Features.Tenants.Queries.GetPaged;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Subscriptions.Queries.GetPaged
{
    public class GetPagedSubscriptionQuery : DataTableRequest, IRequest<DataTableResponse<SubscriptionResponse>>
    {
        public GetPagedSubscriptionQuery(DataTableRequest request)
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
        }
    }

    internal class GetPagedSubscriptionQueryHandler : IRequestHandler<GetPagedSubscriptionQuery, DataTableResponse<SubscriptionResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedSubscriptionQueryHandler> _logger;
        public GetPagedSubscriptionQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedSubscriptionQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<DataTableResponse<SubscriptionResponse>> Handle(GetPagedSubscriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.Subscriptions>().Entities.AsNoTracking();

                if (request.Filters != null && request.Filters.TryGetValue("TenantId", out var tenantIdStr) && !string.IsNullOrEmpty(tenantIdStr))
                {
                    var tenantIds = new List<Guid>();
                    foreach (var val in tenantIdStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Guid.TryParse(val.Trim(), out var tId)) tenantIds.Add(tId);
                    }
                    if (tenantIds.Any())
                    {
                        var tenantSubIds = _unitOfWork.Repository<Domain.Entities.TenantSubscription>().Entities
                            .Where(ts => tenantIds.Contains(ts.TenantId))
                            .Select(ts => ts.SubscriptionId);

                        query = query.Where(s => tenantSubIds.Contains(s.Id));
                    }
                }

                return await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new SubscriptionResponse
                            {
                                Id = e.Id,
                                Name = e.Name,
                                Description = e.Description,
                                MaxApps = e.MaxApps,
                                AllowSeparateDb = e.AllowSeparateDb,
                                MaxUsers = e.MaxUsers,
                                BillingCycle = e.BillingCycle,
                                Price = e.Price,
                                Currency = e.Currency,
                                IsActive = e.IsActive,
                                CreatedBy = e.CreatedBy,
                                CreatedOn = e.CreatedOn,
                                LastModifiedBy = e.LastModifiedBy,
                                LastModifiedOn = e.LastModifiedOn,
                                IPAddress = e.IPAddress,
                                IsDeleted = e.IsDeleted
                            },
                            e => true,
                            new List<string>
                            {
                                nameof(Domain.Entities.Subscriptions.Name),
                                nameof(Domain.Entities.Subscriptions.Description),
                                nameof(Domain.Entities.Subscriptions.MaxUsers),
                                nameof(Domain.Entities.Subscriptions.MaxApps),
                                nameof(Domain.Entities.Subscriptions.CreatedBy),
                                nameof(Domain.Entities.Subscriptions.CreatedOn),
                                nameof(Domain.Entities.Subscriptions.LastModifiedBy),
                                nameof(Domain.Entities.Subscriptions.LastModifiedOn),
                                nameof(Domain.Entities.Subscriptions.IPAddress),
                                nameof(Domain.Entities.Subscriptions.IsDeleted)
                            },
                            cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new DataTableResponse<SubscriptionResponse>
                {
                    Data = new List<SubscriptionResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
