using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.RolePermissions.Commands.Delete
{
    public class DeleteRolePermissionCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    internal class DeleteRolePermissionCommandHandler : IRequestHandler<DeleteRolePermissionCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<DeleteRolePermissionCommandHandler> _logger;

        public DeleteRolePermissionCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<DeleteRolePermissionCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(DeleteRolePermissionCommand command, CancellationToken ct)
        {
            try
            {
                var rolePermission = await _unitOfWork.Repository<Domain.Entities.RolePermission>().GetByIdAsync(command.Id);
                if (rolePermission == null)
                    return await Result<Guid>.FailAsync("Role permission not found.");

                await _unitOfWork.Repository<Domain.Entities.RolePermission>().DeleteAsync(rolePermission);
                await _unitOfWork.Commit(ct);
                return await Result<Guid>.SuccessAsync(command.Id, "Role permission deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting role permission with Id {Id}", command.Id);
                return await Result<Guid>.FailAsync("An error occurred while deleting the role permission.");
            }
        }
    }
}
