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
using System.Text;

namespace SSO.Application.Features.Tenants.Queries.GetPaged
{
    public class GetPagedTenantQuery : DataTableRequest, IRequest<DataTableResponse<TenantResponse>>
    {
        public GetPagedTenantQuery(DataTableRequest request)
        {
            this.SearchValue= request.SearchValue;
            this.Start= request.Start;
            this.Length= request.Length;
            this.Draw= request.Draw;
            this.SortColumn= request.SortColumn;
            this.SortDirection= request.SortDirection;
        }
    }
    internal class GetPagedTenantQueryHandler : IRequestHandler<GetPagedTenantQuery, DataTableResponse<TenantResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedTenantQueryHandler> _logger;
        public GetPagedTenantQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedTenantQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<DataTableResponse<TenantResponse>> Handle(GetPagedTenantQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.Tenants>().Entities
                    .Include(x => x.TenantSubscriptions)
                        .ThenInclude(ts => ts.Subscriptions)
                    .AsNoTracking();

                return await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new TenantResponse
                            {
                                Id = e.Id,
                                Name = e.Name,
                                DatabaseMode = e.DatabaseMode,
                                Code = e.Code,
                                IsActive = e.IsActive,
                                DatabaseProvider = e.DatabaseProvider,
                                DatabaseName = e.DatabaseName,
                                ConnectionString = null,
                                GracePeriodDays = e.GracePeriodDays,
                                Currency = e.Currency,
                                AllowPublicRegistration = e.AllowPublicRegistration,
                                SubscriptionName = e.TenantSubscriptions.Any(x => x.IsActive) ? e.TenantSubscriptions.First(x => x.IsActive).Subscriptions.Name : "No Active Plan",
                                SubscriptionExpiry = e.TenantSubscriptions.Any(x => x.IsActive) ? e.TenantSubscriptions.First(x => x.IsActive).EndDateUtc : (DateTime?)null
                            },
                            e => true,
                            new List<string>
                            {
                                nameof(Domain.Entities.Tenants.Name),
                                nameof(Domain.Entities.Tenants.Code),
                                nameof(Domain.Entities.Tenants.DatabaseMode)
                            },
                            cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new DataTableResponse<TenantResponse>
                {
                    Data = new List<TenantResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }

        }
    }
}
