using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Roles.Commands.AddEdit;
using SSO.Application.Features.Roles.Commands.Delete;
using SSO.Application.Features.Roles.Queries.GetAll;
using SSO.Application.Features.Roles.Queries.GetById;
using SSO.Application.Features.Roles.Queries.GetPaged;
using SSO.Common.Constants.Permission;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers.v1
{
    public class RoleController : BaseApiController<RoleController>
    {
        [HttpGet("Get/{Id}")]
        [Authorize(Policy = Permissions.Roles.View)]
        public async Task<IActionResult> Get(Guid Id)
        {
            var result = await _mediator.Send(new GetByIdRoleQuery { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("GetAll/{tenantId}")]
        [Authorize(Policy = Permissions.Roles.View)]
        public async Task<IActionResult> GetAll(Guid tenantId)
        {
            var result = await _mediator.Send(new GetAllRoleQuery { TenantId = tenantId });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("GetPaged")]
        [Authorize(Policy = Permissions.Roles.View)]
        public async Task<IActionResult> GetPaged(GetPagedRolesQuery request)
        {
            var result = await _mediator.Send(request);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Roles.Create)]
        public async Task<IActionResult> Create(AddEditRolesCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.Roles.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _mediator.Send(new DeleteRoleCommand { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
