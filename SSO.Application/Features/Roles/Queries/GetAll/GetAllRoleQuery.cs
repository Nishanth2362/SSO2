using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Features.Roles.Commands.AddEdit;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Roles.Queries.GetAll
{
    public class GetAllRoleQuery : IRequest<Result<List<RoleResponse>>>
    {
        public Guid TenantId { get; set; }
    }
    internal class GetAllRoleQueryHandler : IRequestHandler<GetAllRoleQuery, Result<List<RoleResponse>>>
    {
        private readonly ILogger<AddEditRolesCommandHandler> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public GetAllRoleQueryHandler(ILogger<AddEditRolesCommandHandler> logger, RoleManager<ApplicationRole> roleManager)
        {
            _logger = logger;
            _roleManager = roleManager;
        }
        public async Task<Result<List<RoleResponse>>> Handle(GetAllRoleQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.TenantId == Guid.Empty)
                {
                    return await Result<List<RoleResponse>>.SuccessAsync(new List<RoleResponse>());
                }
                var roles = await _roleManager.Roles.Where(r => r.TenantId == request.TenantId)
                   .Select(r => new RoleResponse()
                   {
                       Id = r.Id,
                       Name = r.Name!,
                       TenantId = r.TenantId,
                       IsSystemRole = r.IsSystemRole
                   }).ToListAsync();
                return await Result<List<RoleResponse>>.SuccessAsync(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all roles for tenant {TenantId}", request.TenantId);
                return Result<List<RoleResponse>>.Fail("An error occurred while retrieving roles. Please try again later.");
            }
        }
    }
}
