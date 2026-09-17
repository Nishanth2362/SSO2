using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Features.Roles.Commands.AddEdit;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.RolePermissions.Commands.AddEdit
{
    public record RolePermissions
    {
        public Guid PermissionId { get; set; }
    }
    public record AddEditRolePermissionCommand : IRequest<Result<Guid>>
    {
        public Guid RoleId { get; set; }
        public Guid ClientId { get; set; }
        public List<RolePermissions> RolePermissions { get; set; }
    }

    public class AddEditRolePermissionValidator : IRequestValidator<AddEditRolePermissionCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditRolePermissionCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (request.RoleId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.RoleId), ErrorMessage = "Role selection is required." });

            if (request.ClientId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.ClientId), ErrorMessage = "Client selection is required." });

            if (request.RolePermissions == null || request.RolePermissions.Count == 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.RolePermissions), ErrorMessage = "At least one permission must be selected." });
            else if (request.RolePermissions.Any(x => x.PermissionId == Guid.Empty))
                errors.Add(new ValidationError { PropertyName = nameof(request.RolePermissions), ErrorMessage = "Every role permission must reference a valid permission." });
            else if (request.RolePermissions.GroupBy(x => x.PermissionId).Any(g => g.Count() > 1))
                errors.Add(new ValidationError { PropertyName = nameof(request.RolePermissions), ErrorMessage = "Duplicate role permissions are not allowed." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }
    internal class AddEditRolePermissionCommandHandler : IRequestHandler<AddEditRolePermissionCommand, Result<Guid>>
    {
        private readonly ILogger<AddEditRolePermissionCommandHandler> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        public AddEditRolePermissionCommandHandler(ILogger<AddEditRolePermissionCommandHandler> logger, RoleManager<ApplicationRole> roleManager,
            IUnitOfWork<Guid> unitOfWork)
        {
            _logger = logger;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<Guid>> Handle(AddEditRolePermissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (await _unitOfWork.Repository<RolePermission>().Entities.AnyAsync(rp => rp.RoleId == request.RoleId && rp.ApplicationClientId == request.ClientId))
                {
                    _logger.LogInformation("Role permission already exists for RoleId: {RoleId}, ClientId: {ClientId} so Removing All and Adding New", request.RoleId, request.ClientId);
                    var existingRolePermission = _unitOfWork.Repository<RolePermission>().Entities.Where(rp => rp.RoleId == request.RoleId && rp.ApplicationClientId == request.ClientId);
                    foreach (var existing in existingRolePermission)
                    {
                        await _unitOfWork.Repository<RolePermission>().DeleteAsync(existing);

                    }
                    await _unitOfWork.Commit(cancellationToken);
                }

                foreach (var cliams in request.RolePermissions)
                {
                    _logger.LogInformation("Adding Role permission for RoleId: {RoleId}, ClientId: {ClientId} and PermissionId: {PermissionId}", request.RoleId, request.ClientId, cliams.PermissionId);
                    var newRolePermission = new RolePermission
                    {
                        RoleId = request.RoleId,
                        ApplicationClientId = request.ClientId,
                        PermissionId = cliams.PermissionId
                    };
                    await _unitOfWork.Repository<RolePermission>().AddAsync(newRolePermission);
                }
                await _unitOfWork.Commit(cancellationToken);
                return Result<Guid>.Success(request.RoleId, "Role permission added successfully.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding/editing role permission.");
                return Result<Guid>.Fail("An error occurred while processing your request.");
            }
        }
    }
}
