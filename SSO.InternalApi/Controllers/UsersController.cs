using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Users.Queries.GetByTenantName;
using SSO.Shared.Wrapper.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.InternalApi.Controllers;

/// <summary>
/// Internal Users Controller.ff
/// Returns all active users for a given tenant name directly from the database.
/// 
/// Route: GET /internal/users/{tenantName}/{clientId?}
/// </summary>
[ApiController]
[Route("internal/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// GET /internal/users/{tenantName}/{clientId?}
    /// </summary>
    [HttpGet("{tenantName}/{clientId?}")]
    public async Task<IActionResult> GetUsersByTenantName(
        string tenantName,
        string? clientId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
            return BadRequest(new { succeeded = false, message = "tenantName cannot be empty." });

        var result = await _mediator.Send(
            new GetUsersByTenantNameQuery(tenantName, clientId),
            cancellationToken);

        if (result.Succeeded)
            return Ok(result);

        return NotFound(result);
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> GetTenantsByClientId(
        [FromBody] GetTenantsRequest request,
        [FromServices] OpenIddict.Abstractions.IOpenIddictApplicationManager applicationManager,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ClientId))
                return BadRequest(new { succeeded = false, message = "clientId cannot be empty." });

            var client = await applicationManager.FindByClientIdAsync(request.ClientId) as SSO.Domain.Entities.ApplicationClient;
            if (client == null)
                return NotFound(new { succeeded = false, message = $"Client '{request.ClientId}' not found." });

            var result = await _mediator.Send(
                new SSO.Application.Features.Tenants.Queries.GetByClientId.GetTenantsByClientIdQuery(client.Id),
                cancellationToken);

            if (result.Succeeded)
                return Ok(result);

            return NotFound(result);
        }
        catch (Exception ex)
        {
            return Ok(new { succeeded = false, message = ex.Message, innerException = ex.InnerException?.Message, stackTrace = ex.StackTrace });
        }
    }
}

public class GetTenantsRequest
{
    public string ClientId { get; set; }
}
