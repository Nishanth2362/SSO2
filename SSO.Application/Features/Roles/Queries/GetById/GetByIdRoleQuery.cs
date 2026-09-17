
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Roles.Queries.GetById
{
    public record GetByIdRoleQuery : IRequest<Result<RoleResponse>>
    {
        public Guid Id { get; set; }
    }
    internal class GetByIdRoleQueryHandler : IRequestHandler<GetByIdRoleQuery, Result<RoleResponse>>
    {
        private readonly ILogger<GetByIdRoleQueryHandler> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public GetByIdRoleQueryHandler(ILogger<GetByIdRoleQueryHandler> logger, RoleManager<ApplicationRole> roleManager)
        {
            _logger = logger;
            _roleManager = roleManager;
        }
        public async Task<Result<RoleResponse>> Handle(GetByIdRoleQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(request.Id.ToString());
                if (role == null)
                {
                    return Result<RoleResponse>.Fail("Role not found.");
                }
                var response = new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    TenantId = role.TenantId,
                    IsSystemRole = role.IsSystemRole,
                    CreatedBy = role.CreatedBy,
                    CreatedOn = role.CreatedOn,
                    LastModifiedBy = role.LastModifiedBy,
                    LastModifiedOn = role.LastModifiedOn,
                    IPAddress = role.IPAddress,
                    IsDeleted = role.IsDeleted
                };
                return await Result<RoleResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting role by ID {RoleId}", request.Id);
                return Result<RoleResponse>.Fail("An error occurred while retrieving the role. Please try again later.");
            }
        }
    }
}
