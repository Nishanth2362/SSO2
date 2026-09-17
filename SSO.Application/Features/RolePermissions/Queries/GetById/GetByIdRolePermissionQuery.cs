using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SSO.Application.Features.RolePermissions.Queries.GetById
{
    public class GetByIdRolePermissionQuery : IRequest<Result<RolePermissionResponse>>
    {
        public Guid Id { get; set; }
        public GetByIdRolePermissionQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIdRolePermissionQueryHandler : IRequestHandler<GetByIdRolePermissionQuery, Result<RolePermissionResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIdRolePermissionQueryHandler> _logger;

        public GetByIdRolePermissionQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetByIdRolePermissionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<RolePermissionResponse>> Handle(GetByIdRolePermissionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var rolePermission = await _unitOfWork.Repository<Domain.Entities.RolePermission>().Entities
                    .Include(x => x.Role)
                    .Include(x => x.Permission)
                    .Include(x => x.ApplicationClient)
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                if (rolePermission == null)
                    return await Result<RolePermissionResponse>.FailAsync("Role permission not found.");

                var response = new RolePermissionResponse
                {
                    Id = rolePermission.Id,
                    RoleId = rolePermission.RoleId,
                    RoleName = rolePermission.Role.Name!,
                    PermissionId = rolePermission.PermissionId,
                    PermissionName = rolePermission.Permission.Code,
                    ClientApplicationId = rolePermission.ApplicationClientId,
                    ClientApplicationName = rolePermission.ApplicationClient.DisplayName ?? string.Empty
                };

                return await Result<RolePermissionResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting role permission by id {Id}", request.Id);
                return await Result<RolePermissionResponse>.FailAsync("An error occurred while retrieving the role permission.");
            }
        }
    }
}
