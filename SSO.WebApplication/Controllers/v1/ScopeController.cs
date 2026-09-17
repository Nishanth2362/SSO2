using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Scopes.Commands.Import;
using SSO.Common.Constants.Permission;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers.v1
{
    public class ScopeController : BaseApiController<ScopeController>
    {
        private const long ImportFileMaxBytes = 10 * 1024 * 1024;

        [HttpPost("Import")]
        [Authorize(Policy = Permissions.Scope.Import)]
        public async Task<IActionResult> Import(IFormFile file, [FromQuery] Guid? clientId = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (file.Length > ImportFileMaxBytes)
                return BadRequest("The uploaded file exceeds the allowed size of 10 MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not ".xlsx" and not ".xls")
                return BadRequest("Unsupported file type. Allowed types: .xlsx, .xls.");

            using (var stream = file.OpenReadStream())
            {
                var result = await _mediator.Send(new ImportScopesCommand 
                { 
                    Data = stream, 
                    ClientId = clientId 
                });

                if (result.Succeeded)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
        }
    }
}
