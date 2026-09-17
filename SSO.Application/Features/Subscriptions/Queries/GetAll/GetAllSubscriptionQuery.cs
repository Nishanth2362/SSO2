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

namespace SSO.Application.Features.Subscriptions.Queries.GetAll
{
    public class GetAllSubscriptionQuery : IRequest<Result<List<SubscriptionResponse>>>
    {
    }

    internal class GetAllSubscriptionQueryHandler : IRequestHandler<GetAllSubscriptionQuery, Result<List<SubscriptionResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetAllSubscriptionQueryHandler> _logger;

        public GetAllSubscriptionQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetAllSubscriptionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<SubscriptionResponse>>> Handle(GetAllSubscriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var subscriptions = await _unitOfWork.Repository<Domain.Entities.Subscriptions>().Entities
                    .Select(x => new SubscriptionResponse
                    {
                        Id = x.Id,
                        Name = x.Name,
                        MaxUsers = x.MaxUsers,
                        MaxApps = x.MaxApps,
                        AllowSeparateDb = x.AllowSeparateDb,
                        Price = x.Price,
                        Currency = x.Currency,
                        BillingCycle = x.BillingCycle,
                        Description = x.Description,
                        IsActive = x.IsActive
                    }).ToListAsync(cancellationToken);

                return await Result<List<SubscriptionResponse>>.SuccessAsync(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all subscriptions.");
                return await Result<List<SubscriptionResponse>>.FailAsync("An error occurred while retrieving subscriptions.");
            }
        }
    }
}
