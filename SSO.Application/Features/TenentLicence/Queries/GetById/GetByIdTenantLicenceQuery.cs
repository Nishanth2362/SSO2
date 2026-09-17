using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.TenentLicence.Queries.GetById
{
    public class GetByIdTenantLicenceQuery : IRequest<Result<TenantLicenceResponse>>
    {
        public Guid Id { get; set; }

        public GetByIdTenantLicenceQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIdTenantLicenceQueryHandler : IRequestHandler<GetByIdTenantLicenceQuery, Result<TenantLicenceResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIdTenantLicenceQueryHandler> _logger;

        public GetByIdTenantLicenceQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetByIdTenantLicenceQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<TenantLicenceResponse>> Handle(GetByIdTenantLicenceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var license = await _unitOfWork.Repository<Domain.Entities.TenantLicense>().GetByIdAsync(request.Id);
                if (license == null)
                {
                    return await Result<TenantLicenceResponse>.FailAsync("Tenant license not found.");
                }

                var response = new TenantLicenceResponse
                {
                    Id = license.Id,
                    TenantId = license.TenantId,
                    ClientApplicationId = license.ClientApplicationId,
                    LicenseKey = license.LicenseKey,
                    ValidFromUtc = license.ValidFromUtc,
                    ValidToUtc = license.ValidToUtc,
                    MaxUsers = license.MaxUsers,
                    MaxConcurrentUsers = license.MaxConcurrentUsers,
                    FeaturesJson = license.FeaturesJson,
                    IsRevoked = license.IsRevoked
                };

                return await Result<TenantLicenceResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tenant license by id {Id}", request.Id);
                return await Result<TenantLicenceResponse>.FailAsync("An error occurred while retrieving the tenant license.");
            }
        }
    }
}
