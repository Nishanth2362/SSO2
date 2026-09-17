using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using SSO.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Tenants.Queries.GetByClientId
{
    public class GetTenantsByClientIdQuery : IRequest<Result<List<GetTenantsByClientIdResponse>>>
    {
        public Guid ApplicationClientId { get; set; }

        public GetTenantsByClientIdQuery(Guid applicationClientId)
        {
            ApplicationClientId = applicationClientId;
        }
    }

    internal class GetTenantsByClientIdQueryHandler : IRequestHandler<GetTenantsByClientIdQuery, Result<List<GetTenantsByClientIdResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GetTenantsByClientIdQueryHandler> _logger;

        public GetTenantsByClientIdQueryHandler(IUnitOfWork<Guid> unitOfWork, UserManager<ApplicationUser> userManager, ILogger<GetTenantsByClientIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<List<GetTenantsByClientIdResponse>>> Handle(GetTenantsByClientIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var tenantsList = await _unitOfWork.Repository<Domain.Entities.Tenants>()
                    .Entities
                    .Include(t => t.TenantClients)
                    .Where(t => t.TenantClients.Any(tc => tc.ApplicationClientId == request.ApplicationClientId))
                    .Select(t => new GetTenantsByClientIdResponse
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Email = t.Email,
                        Phone = t.Phone,
                        Status = t.IsActive,
                        VendorType = t.TenantSubscriptions
                            .Where(ts => ts.IsActive)
                            .Select(ts => ts.Subscriptions.Name)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                foreach (var tenant in tenantsList)
                {
                    var tenantUsers = await _userManager.Users
                        .Where(u => u.TenantId == tenant.Id)
                        .ToListAsync(cancellationToken);

                    var partners = new List<DeliveryPartnerResponse>();

                    foreach (var user in tenantUsers)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        var userRole = roles.FirstOrDefault() ?? "delivery";

                        partners.Add(new DeliveryPartnerResponse
                        {
                            Id = user.Id,
                            Name = user.Name,
                            Email = user.Email,
                            Phone = user.PhoneNumber ?? string.Empty,
                            Role = userRole
                        });
                    }

                    tenant.DeliveryPartner = partners;
                    tenant.Role = "vendor";
                }

                return await Result<List<GetTenantsByClientIdResponse>>.SuccessAsync(tenantsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tenants by application client id {ApplicationClientId}", request.ApplicationClientId);
                return await Result<List<GetTenantsByClientIdResponse>>.FailAsync("An error occurred while retrieving tenants.");
            }
        }
    }
}
