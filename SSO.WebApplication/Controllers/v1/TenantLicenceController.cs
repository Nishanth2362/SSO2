using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.TenentLicence.Commands.AddEdit;
using SSO.Application.Features.TenentLicence.Commands.Delete;
using SSO.Application.Features.TenentLicence.Queries.GetAll;
using SSO.Application.Features.TenentLicence.Queries.GetById;
using SSO.Application.Features.TenentLicence.Queries.GetPaged;
using SSO.Application.Requests.DataTable;
using SSO.Common.Constants.Permission;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers.v1
{
    public class TenantLicenceController : BaseApiController<TenantLicenceController>
    {
        [HttpGet("Get/{Id}")]
        [Authorize(Policy = Permissions.Tenant.View)]
        public async Task<IActionResult> Get(Guid Id)
        {
            var result = await _mediator.Send(new GetByIdTenantLicenceQuery(Id));
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("GetAll")]
        [Authorize(Policy = Permissions.Tenant.View)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllTenantLicenceQuery());
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("GetPaged")]
        [Authorize(Policy = Permissions.Tenant.View)]
        public async Task<IActionResult> GetPaged(DataTableRequest request)
        {
            var result = await _mediator.Send(new GetPagedTenantLicenceQuery(request));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Tenant.Create)]
        public async Task<IActionResult> Post(AddEditTenantLicenceCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.Tenant.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _mediator.Send(new DeleteTenantLicenceCommand { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
