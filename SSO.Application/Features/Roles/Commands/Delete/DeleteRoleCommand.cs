using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Roles.Commands.Delete
{
    public record DeleteRoleCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; } = Guid.Empty;
    }

    public class DeleteRoleValidator : IRequestValidator<DeleteRoleCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();
            if (request.Id == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.Id), ErrorMessage = "A valid role identifier is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }
    internal class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<Guid>>
    {
        private readonly ILogger<DeleteRoleCommandHandler> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public DeleteRoleCommandHandler(ILogger<DeleteRoleCommandHandler> logger, RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task<Result<Guid>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingRole = await _roleManager.FindByIdAsync(request.Id.ToString());
                if (existingRole == null)
                {
                    return await Result<Guid>.FailAsync("Role not found.");
                }
                var userInRoles = await _userManager.GetUsersInRoleAsync(existingRole.Name!);
                if (userInRoles != null && userInRoles.Count > 0)
                {
                    return await Result<Guid>.FailAsync("Role can't be deleted since it is mapped with existsng users.");
                }
                var result = await _roleManager.DeleteAsync(existingRole);
                if (result.Succeeded)
                {
                    return await Result<Guid>.SuccessAsync(request.Id, "Role deleted successfully.");
                }
                else
                {
                    return await Result<Guid>.FailAsync(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role with Id {RoleId}", request.Id);
                return await Result<Guid>.FailAsync("An error occurred while deleting the role.");
            }
        }
    }
}
