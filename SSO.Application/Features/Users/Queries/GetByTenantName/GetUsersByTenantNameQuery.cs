using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Users.Queries.GetByTenantName
{
    public class GetUsersByTenantNameQuery : IRequest<Result<List<TenantUserDto>>>
    {
        /// <summary>The tenant Name (e.g. "Visitra")</summary>
        public string TenantName { get; set; }

        /// <summary>
        /// Optional: the OpenIddict client_id string (e.g. "visitra-spa-test").
        /// When supplied, validates the client is registered for that tenant before
        /// returning the user list.
        /// </summary>
        public string? ClientId { get; set; }

        public GetUsersByTenantNameQuery(string tenantName, string? clientId = null)
        {
            TenantName = tenantName;
            ClientId = clientId;
        }
    }

    internal class GetUsersByTenantNameQueryHandler
        : IRequestHandler<GetUsersByTenantNameQuery, Result<List<TenantUserDto>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GetUsersByTenantNameQueryHandler> _logger;

        public GetUsersByTenantNameQueryHandler(
            IUnitOfWork<Guid> unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILogger<GetUsersByTenantNameQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Result<List<TenantUserDto>>> Handle(
            GetUsersByTenantNameQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Resolve tenant by Name (fully-qualified to avoid namespace conflict)
                var tenant = await _unitOfWork.Repository<Domain.Entities.Tenants>()
                    .Entities
                    .Include(t => t.TenantClients)
                        .ThenInclude(tc => tc.ApplicationClient)
                    .FirstOrDefaultAsync(
                        t => t.Name.ToLower() == request.TenantName.ToLower(),
                        cancellationToken);

                if (tenant == null)
                    return await Result<List<TenantUserDto>>.FailAsync(
                        $"Tenant '{request.TenantName}' not found.");

                // 2. Optionally validate the ClientId is registered for this tenant
                if (!string.IsNullOrWhiteSpace(request.ClientId))
                {
                    bool clientLinked = tenant.TenantClients
                        .Any(tc => tc.ApplicationClient != null &&
                                   tc.ApplicationClient.ClientId == request.ClientId);

                    if (!clientLinked)
                        return await Result<List<TenantUserDto>>.FailAsync(
                            $"Client '{request.ClientId}' is not registered for tenant '{request.TenantName}'.");
                }

                // 3. Return slim user list — only Id, UserName, Email, IsActive
                var users = await _userManager.Users
                    .Where(u => u.TenantId == tenant.Id && !u.IsDeleted)
                    .Select(u => new TenantUserDto
                    {
                        Id       = u.Id,
                        UserName = u.UserName,
                        Email    = u.Email,
                        IsActive = u.IsActive
                    })
                    .ToListAsync(cancellationToken);

                return await Result<List<TenantUserDto>>.SuccessAsync(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users for tenant '{TenantName}'.", request.TenantName);
                return await Result<List<TenantUserDto>>.FailAsync(
                    "An error occurred while retrieving users.");
            }
        }
    }
}
