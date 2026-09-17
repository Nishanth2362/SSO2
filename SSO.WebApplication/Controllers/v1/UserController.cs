using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Users.Commands.AddEdit;
using SSO.Application.Features.Users.Commands.Delete;
using SSO.Application.Features.Users.Queries.GetAll;
using SSO.Application.Features.Users.Queries.GetById;
using SSO.Application.Features.Users.Queries.GetPaged;
using SSO.Application.Requests.DataTable;
using SSO.Common.Constants.Permission;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers.v1
{
    public class UserController : BaseApiController<UserController>
    {
        [HttpGet("Get/{Id}")]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> Get(Guid Id)
        {
            var result = await _mediator.Send(new GetByIdUserQuery(Id));
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("GetAll")]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllUserQuery());
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("GetPaged")]
        [Authorize(Policy = Permissions.Users.View)]
        public async Task<IActionResult> GetPaged(DataTableRequest request)
        {
            var result = await _mediator.Send(new GetPagedUserQuery(request));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Users.Create)]
        public async Task<IActionResult> Create(AddEditUserCommand command)
        {
            command.Origin = string.IsNullOrWhiteSpace(Request.Headers["origin"])
                ? $"{Request.Scheme}://{Request.Host}"
                : Request.Headers["origin"].ToString();
            var result = await _mediator.Send(command);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.Users.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _mediator.Send(new DeleteUserCommand { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
