using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.RolePermissions.Commands.AddEdit;
using SSO.Application.Features.RolePermissions.Commands.Delete;
using SSO.Application.Features.RolePermissions.Queries.GetAll;
using SSO.Application.Features.RolePermissions.Queries.GetById;
using SSO.Application.Features.RolePermissions.Queries.GetPaged;
using SSO.Application.Features.RolePermissions.Queries.GetRolePermissionsByClient;
using SSO.Application.Requests.DataTable;
using SSO.Common.Constants.Permission;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers.v1
{
    public class RolePermissionController : BaseApiController<RolePermissionController>
    {
        [HttpGet("Get/{Id}")]
        [Authorize(Policy = Permissions.RoleClaims.View)]
        public async Task<IActionResult> Get(Guid Id)
        {
            var result = await _mediator.Send(new GetByIdRolePermissionQuery(Id));
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("GetAll/{roleId}")]
        [Authorize(Policy = Permissions.RoleClaims.View)]
        public async Task<IActionResult> GetAll(Guid roleId)
        {
            var result = await _mediator.Send(new GetAllRolePermissionQuery { RoleId = roleId });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("GetPaged/{roleId}")]
        [Authorize(Policy = Permissions.RoleClaims.View)]
        public async Task<IActionResult> GetPaged(DataTableRequest request, Guid roleId)
        {
            var result = await _mediator.Send(new GetPagedRolePermissionsQuery(request, roleId));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.RoleClaims.Create)]
        public async Task<IActionResult> Post(AddEditRolePermissionCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.RoleClaims.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _mediator.Send(new DeleteRolePermissionCommand { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        [HttpGet("GetByRoleAndClient/{roleId}/{clientId}")]
        public async Task<IActionResult> GetByRoleAndClient(Guid roleId, Guid clientId)
        {
            return Ok(await _mediator.Send(new GetRolePermissionsByClientQuery { RoleId = roleId, ClientId = clientId }));
        }

        [HttpGet("GetAvailablePermissions/{clientId}")]
        public async Task<IActionResult> GetAvailablePermissions(Guid clientId)
        {
            return Ok(await _mediator.Send(new GetAllAvailablePermissionsQuery { ClientId = clientId }));
        }
    }
}
