using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.TenentSubscriptions.Commands.AddEdit;
using SSO.Application.Features.TenentSubscriptions.Commands.Delete;
using SSO.Application.Features.TenentSubscriptions.Queries.GetAll;
using SSO.Application.Features.TenentSubscriptions.Queries.GetById;
using SSO.Application.Features.TenentSubscriptions.Queries.GetPaged;
using SSO.Application.Requests.DataTable;
using SSO.Common.Constants.Permission;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers.v1
{
    public class TenantSubscriptionController : BaseApiController<TenantSubscriptionController>
    {
        [HttpGet("Get/{Id}")]
        [Authorize(Policy = Permissions.Subscription.View)]
        public async Task<IActionResult> Get(Guid Id)
        {
            var result = await _mediator.Send(new GetByIdTenantSubscriptionQuery(Id));
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("GetAll")]
        [Authorize(Policy = Permissions.Subscription.View)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllTenantSubscriptionQuery());
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost("GetPaged")]
        [Authorize(Policy = Permissions.Subscription.View)]
        public async Task<IActionResult> GetPaged(DataTableRequest request)
        {
            var result = await _mediator.Send(new GetPagedTenantSubscriptionQuery(request));
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Subscription.Create)]
        public async Task<IActionResult> Post(AddEditTenantSubscriptionCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.Subscription.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _mediator.Send(new DeleteTenantSubscriptionCommand { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
