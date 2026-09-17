using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;

namespace SSO.Application.Features.Subscriptions.Commands.AddEdit
{
    public class AddEditSubscriptionCommand : IRequest<Result<Guid>>
    {
        public Guid? Id { get; set; } = Guid.Empty;
        public string Name { get; set; }
        public string Description { get; set; }

        public int MaxUsers { get; set; }
        public int MaxApps { get; set; }
        public bool AllowSeparateDb { get; set; }
        public Domain.Enums.SubscriptionBillingCycle BillingCycle { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "INR";
    }

    public class AddEditSubscriptionValidator : IRequestValidator<AddEditSubscriptionCommand>
    {
        private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase) { "INR", "USD", "EUR", "GBP" };

        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Subscription name is required." });
            else if (request.Name.Length > 150)
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Subscription name cannot exceed 150 characters." });

            if (string.IsNullOrWhiteSpace(request.Description))
                errors.Add(new ValidationError { PropertyName = nameof(request.Description), ErrorMessage = "Subscription description is required." });
            else if (request.Description.Length > 500)
                errors.Add(new ValidationError { PropertyName = nameof(request.Description), ErrorMessage = "Subscription description cannot exceed 500 characters." });

            if (request.MaxUsers <= 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.MaxUsers), ErrorMessage = "Max users must be at least 1." });

            if (request.MaxApps <= 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.MaxApps), ErrorMessage = "Max applications must be at least 1." });

            if (request.Price < 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.Price), ErrorMessage = "Price must be non-negative." });

            if (!Enum.IsDefined(typeof(Domain.Enums.SubscriptionBillingCycle), request.BillingCycle))
                errors.Add(new ValidationError { PropertyName = nameof(request.BillingCycle), ErrorMessage = "A valid billing cycle is required." });

            if (string.IsNullOrWhiteSpace(request.Currency) || !AllowedCurrencies.Contains(request.Currency))
                errors.Add(new ValidationError { PropertyName = nameof(request.Currency), ErrorMessage = "A supported currency is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class AddEditSubscriptionCommandHandler : IRequestHandler<AddEditSubscriptionCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<AddEditSubscriptionCommandHandler> _logger;
        public AddEditSubscriptionCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<AddEditSubscriptionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(AddEditSubscriptionCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (command.Id != Guid.Empty && command.Id != null)
                {
                    var subscription = await _unitOfWork.Repository<Domain.Entities.Subscriptions>().GetByIdAsync(command.Id.Value).ConfigureAwait(false);
                    if (subscription == null) return await Result<Guid>.FailAsync("subscription not found.");

                    subscription.Name = command.Name;
                    subscription.Description = command.Description;
                    subscription.MaxUsers = command.MaxUsers;
                    subscription.MaxApps = command.MaxApps;
                    subscription.AllowSeparateDb = command.AllowSeparateDb;
                    subscription.BillingCycle = command.BillingCycle;
                    subscription.Price = command.Price;
                    subscription.Currency = command.Currency;

                    await _unitOfWork.Repository<Domain.Entities.Subscriptions>().UpdateAsync(subscription);
                    await _unitOfWork.Commit(cancellationToken);
                    return await Result<Guid>.SuccessAsync(subscription.Id, "subscription updated successfully.");
                }
                else
                {
                    var newsubscription = new Domain.Entities.Subscriptions
                    {
                        Id = Guid.NewGuid(),
                        Description = command.Description,
                        AllowSeparateDb = command.AllowSeparateDb,
                        MaxApps = command.MaxApps,
                        MaxUsers = command.MaxUsers,
                        Name = command.Name,
                        BillingCycle = command.BillingCycle,
                        Price = command.Price,
                        Currency = command.Currency,
                        IsActive = true
                    };
                    var re = await _unitOfWork.Repository<Domain.Entities.Subscriptions>().AddAsync(newsubscription);
                    var i = await _unitOfWork.Commit(cancellationToken);
                    return await Result<Guid>.SuccessAsync(newsubscription.Id, "subscription created successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding/editing subscription with Name {Name}", command.Name);
                return await Result<Guid>.FailAsync("An error occurred while processing your request. Please try again later.");
            }
        }
    }
}
