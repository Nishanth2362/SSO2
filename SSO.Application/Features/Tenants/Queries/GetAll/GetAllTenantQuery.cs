using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Features.Tenants.Commands.AddEdit;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Tenants.Queries.GetAll
{
    public class GetAllTenantQuery : IRequest<Result<List<TenantResponse>>>
    {
    }
    internal class GetAllTenantQueryHandler : IRequestHandler<GetAllTenantQuery, Result<List<TenantResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<AddEditTenentCommandHandler> _logger;
        public GetAllTenantQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<AddEditTenentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<Result<List<TenantResponse>>> Handle(GetAllTenantQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var tenants = await _unitOfWork.Repository<Domain.Entities.Tenants>().Entities.Select(x => new TenantResponse()
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    DatabaseMode = x.DatabaseMode,
                    DatabaseProvider = x.DatabaseProvider,
                    DatabaseName = x.DatabaseName,
                    ConnectionString = null,
                    IsActive = x.IsActive,
                    GracePeriodDays = x.GracePeriodDays,
                    AllowPublicRegistration = x.AllowPublicRegistration
                }).ToListAsync();

                return await Result<List<TenantResponse>>.SuccessAsync(tenants);    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all tenants.");
                return await Result<List<TenantResponse>>.FailAsync("An error occurred while retrieving tenants.");

            }
        }
    }
}
