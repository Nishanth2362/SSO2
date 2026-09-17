using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SSO.Application.Features.Users.Queries.GetAll;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using OpenIddict.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Users.Queries.GetById
{
    public class GetByIdUserQuery : IRequest<Result<UserResponse>>
    {
        public Guid Id { get; set; }

        public GetByIdUserQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIdUserQueryHandler : IRequestHandler<GetByIdUserQuery, Result<UserResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIdUserQueryHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;

        public GetByIdUserQueryHandler(IUnitOfWork<Guid> unitOfWork, UserManager<ApplicationUser> userManager, ILogger<GetByIdUserQueryHandler> logger, IOpenIddictAuthorizationManager authorizationManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _authorizationManager = authorizationManager;
        }

        public async Task<Result<UserResponse>> Handle(GetByIdUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.Id.ToString());
                if (user == null)
                {
                    return await Result<UserResponse>.FailAsync("User not found.");
                }

                var response = new UserResponse
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FirstName = user.Name,
                    LastName = string.Empty,
                    TenantId = user.TenantId,
                    CreatedAt = user.CreatedOn ?? DateTime.MinValue,
                    UpdatedAt = user.LastModifiedOn ?? DateTime.MinValue,
                    IsActive = user.IsActive
                };
                
                var roles = await _userManager.GetRolesAsync(user);
                response.Roles = roles.ToList();
                
                var authorizations = _authorizationManager.FindAsync(
                    subject: user.Id.ToString(),
                    client: null,
                    status: OpenIddictConstants.Statuses.Valid,
                    type: OpenIddictConstants.AuthorizationTypes.Permanent,
                    scopes: null);

                await foreach (var auth in authorizations)
                {
                    var appIdString = await _authorizationManager.GetApplicationIdAsync(auth);
                    if (Guid.TryParse(appIdString, out Guid appId))
                    {
                        response.ClientIds.Add(appId);
                    }
                }

                return await Result<UserResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user by id {Id}", request.Id);
                return await Result<UserResponse>.FailAsync("An error occurred while retrieving the user.");
            }
        }
    }
}
