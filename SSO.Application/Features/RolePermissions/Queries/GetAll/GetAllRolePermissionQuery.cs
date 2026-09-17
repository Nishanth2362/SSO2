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

namespace SSO.Application.Features.RolePermissions.Queries.GetAll
{
    public class GetAllRolePermissionQuery : IRequest<Result<List<RolePermissionResponse>>>
    {
        public Guid RoleId { get; set; }
    }

    internal class GetAllRolePermissionQueryHandler : IRequestHandler<GetAllRolePermissionQuery, Result<List<RolePermissionResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetAllRolePermissionQueryHandler> _logger;

        public GetAllRolePermissionQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetAllRolePermissionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<RolePermissionResponse>>> Handle(GetAllRolePermissionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var rolePermissions = await _unitOfWork.Repository<Domain.Entities.RolePermission>().Entities
                    .Where(x => x.RoleId == request.RoleId)
                    .Include(x => x.Role)
                    .Include(x => x.Permission)
                    .Include(x => x.ApplicationClient)
                    .Select(x => new RolePermissionResponse
                    {
                        Id = x.Id,
                        RoleId = x.RoleId,
                        RoleName = x.Role.Name!,
                        PermissionId = x.PermissionId,
                        PermissionName = x.Permission.Code,
                        ClientApplicationId = x.ApplicationClientId,
                        ClientApplicationName = x.ApplicationClient.DisplayName ?? string.Empty
                    }).ToListAsync(cancellationToken);

                return await Result<List<RolePermissionResponse>>.SuccessAsync(rolePermissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all role permissions for role {RoleId}", request.RoleId);
                return await Result<List<RolePermissionResponse>>.FailAsync("An error occurred while retrieving role permissions.");
            }
        }
    }
}
