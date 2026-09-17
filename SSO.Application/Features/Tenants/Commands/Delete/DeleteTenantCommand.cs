using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Tenants.Commands.Delete
{
    public class DeleteTenantCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteTenantValidator : IRequestValidator<DeleteTenantCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(DeleteTenantCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();
            if (request.Id == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.Id), ErrorMessage = "A valid tenant identifier is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }
    internal class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<DeleteTenantCommandHandler> _logger;
        public DeleteTenantCommandHandler(IUnitOfWork<Guid> unitOfWork,ILogger<DeleteTenantCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<Result<Guid>> Handle(DeleteTenantCommand command, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Attempting to delete tenant with ID: {TenantId}", command.Id);
                var tenant = await _unitOfWork.Repository<Domain.Entities.Tenants>().GetByIdAsync(command.Id);
                if (tenant == null)
                {
                    _logger.LogWarning("Tenant with ID: {TenantId} not found.", command.Id);
                    return await Result<Guid>.FailAsync("Tenant not found.");
                }
                    
                await _unitOfWork.Repository<Domain.Entities.Tenants>().DeleteAsync(tenant);
                await _unitOfWork.Commit(ct);
                _logger.LogInformation("Tenant with ID: {TenantId} deleted successfully.", tenant.Code);
                return await Result<Guid>.SuccessAsync(command.Id, "Tenant deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return await Result<Guid>.FailAsync(ex.Message);
            }

        }
    }
}
