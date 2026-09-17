using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Tenants.Commands.AddEdit;
using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Requests.DataTable;
using SSO.Application.Requests.Features;
using SSO.Common.Constants.Permission;
using Permissions = SSO.Common.Constants.Permission.Permissions;

namespace SSO.WebApplication.Controllers.v1
{

    public class ClientController : BaseApiController<ClientController>
    {
        private readonly IClientService _clientService;
        public ClientController(IClientService clientService)
        {
            _clientService = clientService;
        }
        [HttpGet("GetAll/{tenantId}")]
        [Authorize(Policy = Permissions.Client.View)]
        public async Task<IActionResult> GetAll(Guid tenantId)
        {
            var result = await _clientService.GetAllAsync(tenantId);
            return Ok(result);
        }

        /// <summary>
        /// Get Paged Clients
        /// </summary>
        [HttpPost("GetPaged")]
        [Authorize(Policy = Permissions.Client.View)]
        public async Task<IActionResult> GetPaged(DataTableRequest request)
        {
            var result = await _clientService.GetClientPaged(request);
            return Ok(result);
        }

        /// <summary>
        /// Post Client
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = Permissions.Client.Create)]
        public async Task<IActionResult> PostClient(ClientRequest command)
        {
            _logger.LogInformation("AddEditClientCommand: {@Command}", command);
            var result = await _clientService.CreateAsync(command);
            if (result.Succeeded)
            {
                _logger.LogInformation("AddEditClientCommand succeeded: {@Result}", result);
                return Ok(result);
            }
            _logger.LogError("AddEditClientCommand failed: {@Result}", result);
            return BadRequest(result);
        }
        [HttpDelete("{Id}")]
        [Authorize(Policy = Permissions.Client.Delete)]
        public async Task<IActionResult> Delete(Guid Id)
        {
            var result = await _clientService.DeleteAsync(Id);
            if (result.Succeeded)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
