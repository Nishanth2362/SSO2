using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.TenentSubscriptions.Commands.Delete
{
    public class DeleteTenantSubscriptionCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    internal class DeleteTenantSubscriptionCommandHandler : IRequestHandler<DeleteTenantSubscriptionCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<DeleteTenantSubscriptionCommandHandler> _logger;

        public DeleteTenantSubscriptionCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<DeleteTenantSubscriptionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(DeleteTenantSubscriptionCommand command, CancellationToken ct)
        {
            try
            {
                var subscription = await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().GetByIdAsync(command.Id);
                if (subscription == null)
                    return await Result<Guid>.FailAsync("Tenant subscription not found.");

                await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().DeleteAsync(subscription);
                await _unitOfWork.Commit(ct);
                return await Result<Guid>.SuccessAsync(command.Id, "Tenant subscription deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting tenant subscription with Id {Id}", command.Id);
                return await Result<Guid>.FailAsync("An error occurred while deleting the tenant subscription.");
            }
        }
    }
}
