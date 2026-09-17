using Microsoft.Extensions.Logging;
using SSO.Application.Features.Tenants.Commands.AddEdit;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using SSO.Domain.Entities;

namespace SSO.Application.Features.TenentSubscriptions.Commands.AddEdit
{
    public class AddEditTenantSubscriptionCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; } = Guid.Empty;
        public Guid TenantId { get; set; }
        public Guid SubscriptionId { get; set; }

        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }

        public bool IsActive { get; set; }
    }

    public class AddEditTenantSubscriptionValidator : IRequestValidator<AddEditTenantSubscriptionCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditTenantSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (request.TenantId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.TenantId), ErrorMessage = "Tenant selection is required." });

            if (request.SubscriptionId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.SubscriptionId), ErrorMessage = "Subscription plan selection is required." });

            if (request.StartDateUtc == default)
                errors.Add(new ValidationError { PropertyName = nameof(request.StartDateUtc), ErrorMessage = "Start date is required." });

            if (request.EndDateUtc == default)
                errors.Add(new ValidationError { PropertyName = nameof(request.EndDateUtc), ErrorMessage = "End date is required." });

            if (request.EndDateUtc <= request.StartDateUtc)
                errors.Add(new ValidationError { PropertyName = nameof(request.EndDateUtc), ErrorMessage = "End date must be after start date." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }
    internal class AddEditTenantSubscriptionCommandHandler : IRequestHandler<AddEditTenantSubscriptionCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<AddEditTenantSubscriptionCommandHandler> _logger;
        public AddEditTenantSubscriptionCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<AddEditTenantSubscriptionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(AddEditTenantSubscriptionCommand command, CancellationToken ct)
        {
            try
            {
                if (command.IsActive)
                {
                    var activeSubscriptions = await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().Entities
                        .Where(x => x.TenantId == command.TenantId && x.IsActive && x.Id != command.Id)
                        .ToListAsync(ct);
                    foreach (var sub in activeSubscriptions)
                    {
                        sub.IsActive = false;
                        await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().UpdateAsync(sub).ConfigureAwait(false);
                    }
                }

                if (command.Id != Guid.Empty)
                {
                    var tenantSubscription = await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().GetByIdAsync(command.Id).ConfigureAwait(false);
                    if (tenantSubscription == null)
                        return await Result<Guid>.FailAsync("TenantSubscription not found.");
                   tenantSubscription.EndDateUtc = command.EndDateUtc.ToUniversalTime();
                    tenantSubscription.TenantId = command.TenantId;
                    tenantSubscription.StartDateUtc = command.StartDateUtc.ToUniversalTime();
                    tenantSubscription.SubscriptionId = command.SubscriptionId;
                    tenantSubscription.IsActive = command.IsActive;
                    await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().UpdateAsync(tenantSubscription).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(tenantSubscription.Id, "TenantSubscription updated successfully.").ConfigureAwait(false);
                }
                else
                {
                    var newTenantSubscription = new Domain.Entities.TenantSubscription
                    {
                        Id = Guid.NewGuid(),
                        EndDateUtc = command.EndDateUtc.ToUniversalTime(),
                        StartDateUtc = command.StartDateUtc.ToUniversalTime(),
                        SubscriptionId = command.SubscriptionId,
                        TenantId = command.TenantId,
                        IsActive = command.IsActive
                    };
                    var re = await _unitOfWork.Repository<Domain.Entities.TenantSubscription>().AddAsync(newTenantSubscription).ConfigureAwait(false);
                    
                    // Generate Invoice
                    var subPlan = await _unitOfWork.Repository<Domain.Entities.Subscriptions>().GetByIdAsync(command.SubscriptionId);
                    if (subPlan != null)
                    {
                        var invoice = new Invoice
                        {
                            Id = Guid.NewGuid(),
                            TenantSubscriptionId = newTenantSubscription.Id,
                            InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                            InvoiceDate = DateTime.UtcNow,
                            DueDate = DateTime.UtcNow.AddDays(15),
                            Amount = subPlan.Price,
                            TaxAmount = subPlan.Price * 0.1m, // Example 10% tax
                            TotalAmount = subPlan.Price * 1.1m,
                            Status = InvoiceStatus.Pending
                        };
                        await _unitOfWork.Repository<Invoice>().AddAsync(invoice);
                    }

                    var i = await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(newTenantSubscription.Id, "TenantSubscription and Invoice created successfully.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding/editing TenantSubscription with code {Id}", command.Id);
                return await Result<Guid>.FailAsync("An error occurred while processing your request. Please try again later.").ConfigureAwait(false);
            }
        }
    }
}
