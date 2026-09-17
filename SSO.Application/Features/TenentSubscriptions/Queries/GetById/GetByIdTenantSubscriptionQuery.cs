using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.TenentSubscriptions.Queries.GetById
{
    public class GetByIdTenantSubscriptionQuery : IRequest<Result<TenantSubscriptionResponse>>
    {
        public Guid Id { get; set; }

        public GetByIdTenantSubscriptionQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIdTenantSubscriptionQueryHandler : IRequestHandler<GetByIdTenantSubscriptionQuery, Result<TenantSubscriptionResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIdTenantSubscriptionQueryHandler> _logger;

        public GetByIdTenantSubscriptionQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetByIdTenantSubscriptionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<TenantSubscriptionResponse>> Handle(GetByIdTenantSubscriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var subscription = await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().Entities
                    .Include(x => x.Tenants)
                    .Include(x => x.Subscriptions)
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                if (subscription == null)
                {
                    return await Result<TenantSubscriptionResponse>.FailAsync("Tenant subscription not found.");
                }

                var response = new TenantSubscriptionResponse
                {
                    Id = subscription.Id,
                    TenantId = subscription.TenantId,
                    TenantName = subscription.Tenants?.Name ?? string.Empty,
                    SubscriptionId = subscription.SubscriptionId,
                    SubscriptionName = subscription.Subscriptions?.Name ?? string.Empty,
                    StartDateUtc = subscription.StartDateUtc,
                    EndDateUtc = subscription.EndDateUtc,
                    IsActive = subscription.IsActive
                };

                return await Result<TenantSubscriptionResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tenant subscription by id {Id}", request.Id);
                return await Result<TenantSubscriptionResponse>.FailAsync("An error occurred while retrieving the tenant subscription.");
            }
        }
    }
}
