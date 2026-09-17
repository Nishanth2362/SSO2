using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Features.Subscriptions.Queries.GetPaged;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.TenentLicence.Queries.GetPaged
{
    public class GetPagedTenantLicenceQuery : DataTableRequest, IRequest<DataTableResponse<TenantLicenceResponse>>
    {
        public GetPagedTenantLicenceQuery(DataTableRequest request)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
        }
    }
    internal class GetPagedTenantLicenceQueryHandler : IRequestHandler<GetPagedTenantLicenceQuery, DataTableResponse<TenantLicenceResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedTenantLicenceQueryHandler> _logger;
        public GetPagedTenantLicenceQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedTenantLicenceQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<DataTableResponse<TenantLicenceResponse>> Handle(GetPagedTenantLicenceQuery request, CancellationToken cancellationToken)
        {

            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.TenantLicense>().Entities.AsNoTracking();
                return await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new TenantLicenceResponse
                            {
                                MaxUsers = e.MaxUsers,
                                MaxConcurrentUsers = e.MaxConcurrentUsers,
                                ClientApplicationId = e.ClientApplicationId,
                                FeaturesJson = e.FeaturesJson,
                                Id = e.Id,
                                IsRevoked = e.IsRevoked,
                                LicenseKey = e.LicenseKey,
                                TenantId = e.TenantId,
                                ValidFromUtc = e.ValidFromUtc,
                                ValidToUtc = e.ValidToUtc
                            },
                            e => true,
                            new List<string>
                            {
                                nameof(Domain.Entities.TenantLicense.LicenseKey),
                                nameof(Domain.Entities.TenantLicense.MaxConcurrentUsers),
                                nameof(Domain.Entities.TenantLicense.MaxUsers)
                            },
                            cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new DataTableResponse<TenantLicenceResponse>
                {
                    Data = new List<TenantLicenceResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
