using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Subscriptions.Queries.GetById
{
    public class GetByIdSubscriptionQuery : IRequest<Result<SubscriptionResponse>>
    {
        public Guid Id { get; set; }

        public GetByIdSubscriptionQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIdSubscriptionQueryHandler : IRequestHandler<GetByIdSubscriptionQuery, Result<SubscriptionResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIdSubscriptionQueryHandler> _logger;

        public GetByIdSubscriptionQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetByIdSubscriptionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<SubscriptionResponse>> Handle(GetByIdSubscriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var subscription = await _unitOfWork.Repository<Domain.Entities.Subscriptions>().GetByIdAsync(request.Id);
                if (subscription == null)
                {
                    return await Result<SubscriptionResponse>.FailAsync("Subscription not found.");
                }

                var response = new SubscriptionResponse
                {
                    Id = subscription.Id,
                    Name = subscription.Name,
                    MaxUsers = subscription.MaxUsers,
                    MaxApps = subscription.MaxApps,
                    AllowSeparateDb = subscription.AllowSeparateDb,
                    BillingCycle = subscription.BillingCycle,
                    Price = subscription.Price,
                    Currency = subscription.Currency,
                    Description = subscription.Description,
                    IsActive = subscription.IsActive
                };

                return await Result<SubscriptionResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting subscription by id {Id}", request.Id);
                return await Result<SubscriptionResponse>.FailAsync("An error occurred while retrieving the subscription.");
            }
        }
    }
}
