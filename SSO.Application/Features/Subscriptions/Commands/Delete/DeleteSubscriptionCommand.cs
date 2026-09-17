using Microsoft.Extensions.Logging;
using SSO.Application.Features.Subscriptions.Commands.AddEdit;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Subscriptions.Commands.Delete
{
    public class DeleteSubscriptionCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteSubscriptionValidator : IRequestValidator<DeleteSubscriptionCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(DeleteSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();
            if (request.Id == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.Id), ErrorMessage = "A valid subscription identifier is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class DeleteSubscriptionCommandHandler : IRequestHandler<DeleteSubscriptionCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<DeleteSubscriptionCommandHandler> _logger;
        public DeleteSubscriptionCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<DeleteSubscriptionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<Result<Guid>> Handle(DeleteSubscriptionCommand command, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Attempting to delete subscription with ID: {subscriptionId}", command.Id);
                var subscription = await _unitOfWork.Repository<Domain.Entities.Subscriptions>().GetByIdAsync(command.Id);
                if (subscription == null)
                {
                    _logger.LogWarning("subscription with ID: {subscriptionId} not found.", command.Id);
                    return await Result<Guid>.FailAsync("subscription not found.");
                }

                await _unitOfWork.Repository<Domain.Entities.Subscriptions>().DeleteAsync(subscription);
                await _unitOfWork.Commit(ct);
                _logger.LogInformation("subscription with ID: {subscriptionId} deleted successfully.", subscription.Id);
                return await Result<Guid>.SuccessAsync(command.Id, "subscription deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return await Result<Guid>.FailAsync(ex.Message);
            }

        }
    }
}
