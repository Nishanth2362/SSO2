using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Templates.Commands.AddEdit;
using SSO.Application.Features.Templates.Commands.Delete;
using SSO.Application.Features.Templates.Queries.GetAll;
using SSO.Application.Features.Templates.Queries.GetById;
using SSO.Common.Constants.Permission;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers.v1
{
    public class TemplateController : BaseApiController<TemplateController>
    {
        [HttpGet("Get/{Id}")]
        [Authorize(Policy = Permissions.Templates.View)]
        public async Task<IActionResult> Get(Guid Id)
        {
            var result = await _mediator.Send(new GetByIDTemplateQuery(Id));
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpGet("GetAll")]
        [Authorize(Policy = Permissions.Templates.View)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetAllTemplateQuery());
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Templates.Create)]
        public async Task<IActionResult> Post(AddEditTemplateCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.Templates.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _mediator.Send(new DeleteTemplateCommand { Id = Id });
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
