using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Roles.Commands.AddEdit
{
    public record AddEditRolesCommand:IRequest<Result<Guid>>
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid TenantId { get; set; }
        public bool IsSystemRole { get; set; }
    }

    public class AddEditRolesValidator : IRequestValidator<AddEditRolesCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditRolesCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Role name is required." });
            else if (request.Name.Length > 50)
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Role name cannot exceed 50 characters." });

            if (request.TenantId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.TenantId), ErrorMessage = "Tenant selection is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class AddEditRolesCommandHandler : IRequestHandler<AddEditRolesCommand, Result<Guid>>
    {
        private readonly ILogger<AddEditRolesCommandHandler>   _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public AddEditRolesCommandHandler(ILogger<AddEditRolesCommandHandler> logger, RoleManager<ApplicationRole> roleManager)
        {
            _logger = logger;
            _roleManager = roleManager;
        }
        public async Task<Result<Guid>> Handle(AddEditRolesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if(request.Id == null || request.Id == Guid.Empty)
                {
                    var newRole = new ApplicationRole
                    {
                        Name = request.Name,
                        Description = request.Description ?? "",
                        TenantId = request.TenantId,
                        IsSystemRole = request.IsSystemRole
                    };
                    var result = await _roleManager.CreateAsync(newRole);
                    if (result.Succeeded)
                    {
                        return await Result<Guid>.SuccessAsync(newRole.Id, "Role created successfully.");
                    }
                    else
                    {
                        return await Result<Guid>.FailAsync(string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    var existingRole = await _roleManager.FindByIdAsync(request.Id.ToString());
                    if (existingRole == null)
                    {
                        return await Result<Guid>.FailAsync("Role not found.");
                    }
                    existingRole.TenantId=request.TenantId;
                    existingRole.Name = request.Name;
                    existingRole.Description = request.Description ?? "";
                    existingRole.IsSystemRole = request.IsSystemRole;
                    var result = await _roleManager.UpdateAsync(existingRole);
                    if (result.Succeeded)
                    {
                        return await Result<Guid>.SuccessAsync(existingRole.Id, "Role updated successfully.");
                    }
                    else
                    {
                        return await Result<Guid>.FailAsync(string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding/editing role with Id: {RoleId}", request.Id);
                return await Result<Guid>.FailAsync("An error occurred while processing your request.");
            }
        }
    }
}
