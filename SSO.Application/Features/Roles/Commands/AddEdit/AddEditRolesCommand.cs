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

        /// <summary>Mandatory change description when editing an existing role.</summary>
        public string? Remarks { get; set; }
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

            if (request.Id != null && request.Id != Guid.Empty)
            {
                if (string.IsNullOrWhiteSpace(request.Remarks))
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "A change description (remarks) is required when editing." });
                else if (request.Remarks.Trim().Length < 5)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "Remarks must be at least 5 characters." });
                else if (request.Remarks.Length > 500)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "Remarks cannot exceed 500 characters." });
            }

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class AddEditRolesCommandHandler : IRequestHandler<AddEditRolesCommand, Result<Guid>>
    {
        private readonly ILogger<AddEditRolesCommandHandler> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SSO.Application.Interfaces.Services.ICurrentUserService _currentUserService;

        public AddEditRolesCommandHandler(
            ILogger<AddEditRolesCommandHandler> logger, 
            RoleManager<ApplicationRole> roleManager,
            SSO.Application.Interfaces.Services.ICurrentUserService currentUserService)
        {
            _logger = logger;
            _roleManager = roleManager;
            _currentUserService = currentUserService;
        }

        public async Task<Result<Guid>> Handle(AddEditRolesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Enforce tenant isolation and system role checks
                if (!_currentUserService.IsMasterTenant)
                {
                    if (request.TenantId != _currentUserService.TenantId)
                    {
                        return await Result<Guid>.FailAsync("Access denied: You cannot create or modify roles outside of your tenant.");
                    }
                    if (request.IsSystemRole)
                    {
                        return await Result<Guid>.FailAsync("Access denied: System roles can only be created by Master Administrators.");
                    }
                }

                if (request.Id == null || request.Id == Guid.Empty)
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

                    if (!_currentUserService.IsMasterTenant)
                    {
                        if (existingRole.TenantId != _currentUserService.TenantId)
                        {
                            return await Result<Guid>.FailAsync("Access denied: Target role does not belong to your tenant.");
                        }
                        if (existingRole.IsSystemRole)
                        {
                            return await Result<Guid>.FailAsync("Access denied: System roles cannot be modified by Tenant Administrators.");
                        }
                    }

                    existingRole.TenantId = request.TenantId;
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
