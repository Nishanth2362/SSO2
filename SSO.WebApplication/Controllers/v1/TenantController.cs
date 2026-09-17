using DocumentFormat.OpenXml.Office.CoverPageProps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Tenants.Commands.AddEdit;
using SSO.Application.Features.Tenants.Queries.GetAll;
using SSO.Application.Features.Tenants.Queries.GetById;
using SSO.Common.Constants.Permission;
using SSO.Shared.Wrapper.Mediator;

namespace SSO.WebApplication.Controllers.v1
{

    public class TenantController : BaseApiController<TenantController>
    {
        [HttpGet("Get/{Id}")]
        [Authorize(Policy = Permissions.Tenant.View)]
        public async Task<IActionResult> Get(Guid Id)
        {
            var result = await _mediator.Send(new GetByIdTenantQuery(Id));
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Get All Tenants
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAll")]
        [Authorize(Policy = Permissions.Tenant.View)]
        public async Task<IActionResult> GetTenants()
        {
            _logger.LogInformation("GetTenants called");
            var result = await _mediator.Send(new GetAllTenantQuery());
            if (result.Succeeded)
            {
                _logger.LogInformation("GetTenants succeeded: {@Result}", result);
                return Ok(result);
            }
            _logger.LogError("GetTenants failed: {@Result}", result);
            return BadRequest(result.Messages[0]);
        }

        /// <summary>
        /// Post Tenant
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = Permissions.Tenant.Create)]
        public async Task<IActionResult> PostTenant(AddEditTenentCommand command)
        {
            _logger.LogInformation("AddEditTenentCommand: {@Command}", command);
            var result = await _mediator.Send(command);
            if (result.Succeeded)
            {
                _logger.LogInformation("AddEditTenentCommand succeeded: {@Result}", result);
                return Ok(result);
            }
            _logger.LogError("AddEditTenentCommand failed: {@Result}", result);
            return BadRequest(result);
        }

        [HttpPost("GetPaged")]
        [Authorize(Policy = Permissions.Tenant.View)]
        public async Task<IActionResult> GetPaged(SSO.Application.Requests.DataTable.DataTableRequest request)
        {
            var result = await _mediator.Send(new SSO.Application.Features.Tenants.Queries.GetPaged.GetPagedTenantQuery(request));
            return Ok(result);
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.Tenant.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _mediator.Send(new SSO.Application.Features.Tenants.Commands.Delete.DeleteTenantCommand { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
