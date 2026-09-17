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

namespace SSO.Application.Features.TenentLicence.Queries.GetAll
{
    public class GetAllTenantLicenceQuery : IRequest<Result<List<TenantLicenceResponse>>>
    {
    }

    internal class GetAllTenantLicenceQueryHandler : IRequestHandler<GetAllTenantLicenceQuery, Result<List<TenantLicenceResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetAllTenantLicenceQueryHandler> _logger;

        public GetAllTenantLicenceQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetAllTenantLicenceQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<TenantLicenceResponse>>> Handle(GetAllTenantLicenceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var licenses = await _unitOfWork.Repository<Domain.Entities.TenantLicense>().Entities
                    .Select(x => new TenantLicenceResponse
                    {
                        Id = x.Id,
                        TenantId = x.TenantId,
                        ClientApplicationId = x.ClientApplicationId,
                        LicenseKey = x.LicenseKey,
                        ValidFromUtc = x.ValidFromUtc,
                        ValidToUtc = x.ValidToUtc,
                        MaxUsers = x.MaxUsers,
                        MaxConcurrentUsers = x.MaxConcurrentUsers,
                        FeaturesJson = x.FeaturesJson,
                        IsRevoked = x.IsRevoked
                    }).ToListAsync(cancellationToken);

                return await Result<List<TenantLicenceResponse>>.SuccessAsync(licenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all tenant licenses.");
                return await Result<List<TenantLicenceResponse>>.FailAsync("An error occurred while retrieving tenant licenses.");
            }
        }
    }
}
