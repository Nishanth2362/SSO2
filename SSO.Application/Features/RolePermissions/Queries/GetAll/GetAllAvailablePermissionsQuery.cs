using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.RolePermissions.Queries.GetAll
{
    public class GetAllAvailablePermissionsQuery : IRequest<Result<List<PermissionResponse>>>
    {
        public Guid? ClientId { get; set; }
    }

    internal class GetAllAvailablePermissionsQueryHandler : IRequestHandler<GetAllAvailablePermissionsQuery, Result<List<PermissionResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetAllAvailablePermissionsQueryHandler> _logger;
        private readonly IClientService _clientService;

        public GetAllAvailablePermissionsQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetAllAvailablePermissionsQueryHandler> logger, IClientService clientService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _clientService = clientService;
        }

        public async Task<Result<List<PermissionResponse>>> Handle(GetAllAvailablePermissionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var defaultClient = await _clientService.GetDefaultClientAsync(cancellationToken);

                var query = _unitOfWork.Repository<Domain.Entities.Permission>().Entities.AsNoTracking();

                if (request.ClientId.HasValue)
                {
                    query = query.Where(x => x.ClientApplicationId == defaultClient.Data.Id || x.ClientApplicationId == request.ClientId);
                }
                else
                {
                    // If no client selected, maybe just return SSO permissions or nothing
                    // Based on "Do not load permissions on initial load", we might want to return empty or only SSO
                    query = query.Where(x => x.ClientApplicationId == defaultClient.Data.Id); 
                }

                var permissions = await query
                    .Select(x => new PermissionResponse
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Description = x.Description,
                        Category = x.ClientApplicationId == defaultClient.Data.Id ? "SSO" : x.Code.Split('.')[1]
                    }).ToListAsync(cancellationToken);

                return await Result<List<PermissionResponse>>.SuccessAsync(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all available permissions.");
                return await Result<List<PermissionResponse>>.FailAsync("An error occurred while retrieving available permissions.");
            }
        }
    }
}
