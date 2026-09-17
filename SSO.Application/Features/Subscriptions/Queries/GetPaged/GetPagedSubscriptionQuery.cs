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
                                IsActive = e.IsActive
                            },
                            e => true,
                            new List<string>
                            {
                                nameof(Domain.Entities.Subscriptions.Name),
                                nameof(Domain.Entities.Subscriptions.Description),
                                nameof(Domain.Entities.Subscriptions.MaxUsers),
                                nameof(Domain.Entities.Subscriptions.MaxApps)
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
