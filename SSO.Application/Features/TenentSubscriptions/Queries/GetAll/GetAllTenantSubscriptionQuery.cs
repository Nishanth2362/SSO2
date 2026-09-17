using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.TenentSubscriptions.Queries.GetAll
{
    public class GetAllTenantSubscriptionQuery : IRequest<Result<List<TenantSubscriptionResponse>>>
    {
    }

    internal class GetAllTenantSubscriptionQueryHandler : IRequestHandler<GetAllTenantSubscriptionQuery, Result<List<TenantSubscriptionResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetAllTenantSubscriptionQueryHandler> _logger;

        public GetAllTenantSubscriptionQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetAllTenantSubscriptionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<TenantSubscriptionResponse>>> Handle(GetAllTenantSubscriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var subscriptions = await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().Entities
                    .Include(x => x.Tenants)
                    .Include(x => x.Subscriptions)
                    .Select(x => new TenantSubscriptionResponse
                    {
                        Id = x.Id,
                        TenantId = x.TenantId,
                        TenantName = x.Tenants != null ? x.Tenants.Name : string.Empty,
                        SubscriptionId = x.SubscriptionId,
                        SubscriptionName = x.Subscriptions != null ? x.Subscriptions.Name : string.Empty,
                        StartDateUtc = x.StartDateUtc,
                        EndDateUtc = x.EndDateUtc,
                        IsActive = x.IsActive
                    }).ToListAsync(cancellationToken);

                return await Result<List<TenantSubscriptionResponse>>.SuccessAsync(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all tenant subscriptions.");
                return await Result<List<TenantSubscriptionResponse>>.FailAsync("An error occurred while retrieving tenant subscriptions.");
            }
        }
    }
}
