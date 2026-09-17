using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.TenentLicence.Commands.AddEdit
{
    public class AddEditTenantLicenceCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; } = Guid.Empty;
        public Guid TenantId { get; set; }
        public Guid ClientApplicationId { get; set; }
        public string LicenseKey { get; set; } = null!;
        public DateTime ValidFromUtc { get; set; }
        public DateTime ValidToUtc { get; set; }
        public int MaxUsers { get; set; }
        public int MaxConcurrentUsers { get; set; }
        public string FeaturesJson { get; set; } = null!;
        public bool IsRevoked { get; set; }
    }

    public class AddEditTenantLicenceValidator : IRequestValidator<AddEditTenantLicenceCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditTenantLicenceCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (request.TenantId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.TenantId), ErrorMessage = "Tenant selection is required." });

            if (request.ClientApplicationId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.ClientApplicationId), ErrorMessage = "Client application selection is required." });

            if (string.IsNullOrWhiteSpace(request.LicenseKey))
                errors.Add(new ValidationError { PropertyName = nameof(request.LicenseKey), ErrorMessage = "License key is required." });
            else if (request.LicenseKey.Length > 200)
                errors.Add(new ValidationError { PropertyName = nameof(request.LicenseKey), ErrorMessage = "License key cannot exceed 200 characters." });

            if (request.ValidToUtc <= request.ValidFromUtc)
                errors.Add(new ValidationError { PropertyName = nameof(request.ValidToUtc), ErrorMessage = "Validity end date must be after start date." });

            if (request.MaxUsers <= 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.MaxUsers), ErrorMessage = "Max users must be at least 1." });

            if (request.MaxConcurrentUsers <= 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.MaxConcurrentUsers), ErrorMessage = "Max concurrent users must be at least 1." });
            else if (request.MaxConcurrentUsers > request.MaxUsers)
                errors.Add(new ValidationError { PropertyName = nameof(request.MaxConcurrentUsers), ErrorMessage = "Max concurrent users cannot exceed max users." });

            if (!string.IsNullOrWhiteSpace(request.FeaturesJson))
            {
                try
                {
                    JsonDocument.Parse(request.FeaturesJson);
                }
                catch (JsonException)
                {
                    errors.Add(new ValidationError { PropertyName = nameof(request.FeaturesJson), ErrorMessage = "Features JSON must be valid JSON." });
                }
            }

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class AddEditTenantLicenceCommandHandler : IRequestHandler<AddEditTenantLicenceCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<AddEditTenantLicenceCommandHandler> _logger;

        public AddEditTenantLicenceCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<AddEditTenantLicenceCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(AddEditTenantLicenceCommand command, CancellationToken ct)
        {
            try
            {
                if (command.Id != Guid.Empty)
                {
                    var license = await _unitOfWork.Repository<Domain.Entities.TenantLicense>().GetByIdAsync(command.Id).ConfigureAwait(false);
                    if (license == null)
                        return await Result<Guid>.FailAsync("Tenant license not found.");

                    license.TenantId = command.TenantId;
                    license.ClientApplicationId = command.ClientApplicationId;
                    license.LicenseKey = command.LicenseKey;
                    license.ValidFromUtc = command.ValidFromUtc;
                    license.ValidToUtc = command.ValidToUtc;
                    license.MaxUsers = command.MaxUsers;
                    license.MaxConcurrentUsers = command.MaxConcurrentUsers;
                    license.FeaturesJson = command.FeaturesJson;
                    license.IsRevoked = command.IsRevoked;

                    await _unitOfWork.Repository<Domain.Entities.TenantLicense>().UpdateAsync(license).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(license.Id, "Tenant license updated successfully.").ConfigureAwait(false);
                }
                else
                {
                    var newLicense = new Domain.Entities.TenantLicense
                    {
                        Id = Guid.NewGuid(),
                        TenantId = command.TenantId,
                        ClientApplicationId = command.ClientApplicationId,
                        LicenseKey = command.LicenseKey,
                        ValidFromUtc = command.ValidFromUtc,
                        ValidToUtc = command.ValidToUtc,
                        MaxUsers = command.MaxUsers,
                        MaxConcurrentUsers = command.MaxConcurrentUsers,
                        FeaturesJson = command.FeaturesJson,
                        IsRevoked = command.IsRevoked
                    };

                    await _unitOfWork.Repository<Domain.Entities.TenantLicense>().AddAsync(newLicense).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(newLicense.Id, "Tenant license created successfully.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding/editing tenant license for tenant {TenantId}", command.TenantId);
                return await Result<Guid>.FailAsync("An error occurred while processing your request. Please try again later.").ConfigureAwait(false);
            }
        }
    }
}
