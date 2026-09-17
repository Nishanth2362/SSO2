using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace SSO.Application.Features.Tenants.Queries.GetById
{
    public class GetByIdTenantQuery : IRequest<Result<TenantResponse>>
    {
        public Guid Id { get; set; }

        public GetByIdTenantQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIdTenantQueryHandler : IRequestHandler<GetByIdTenantQuery, Result<TenantResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIdTenantQueryHandler> _logger;

        public GetByIdTenantQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetByIdTenantQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<TenantResponse>> Handle(GetByIdTenantQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var tenant = await _unitOfWork.Repository<Domain.Entities.Tenants>()
                    .Entities.Include(t => t.TenantClients)
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
                if (tenant == null)
                {
                    return await Result<TenantResponse>.FailAsync("Tenant not found.");
                }

                var response = new TenantResponse
                {
                    Id = tenant.Id,
                    Code = tenant.Code,
                    Name = tenant.Name,
                    DatabaseMode = tenant.DatabaseMode,
                    DatabaseProvider = tenant.DatabaseProvider,
                    DatabaseName = tenant.DatabaseName,
                    ConnectionString = null,
                    IsActive = tenant.IsActive,
                    LogoUrl = tenant.LogoUrl,
                    FaviconUrl = tenant.FaviconUrl,
                    Email = tenant.Email,
                    Phone = tenant.Phone,
                    Website = tenant.Website,
                    BillingAddress = tenant.BillingAddress,
                    GracePeriodDays = tenant.GracePeriodDays,
                    Currency = tenant.Currency,
                    AllowPublicRegistration = tenant.AllowPublicRegistration,
                    ClientIds = tenant.TenantClients?.Select(tc => tc.ApplicationClientId).ToList()
                };

                return await Result<TenantResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tenant by id {Id}", request.Id);
                return await Result<TenantResponse>.FailAsync("An error occurred while retrieving the tenant.");
            }
        }
    }
}
