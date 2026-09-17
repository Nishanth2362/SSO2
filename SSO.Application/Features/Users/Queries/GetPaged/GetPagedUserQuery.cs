using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Users.Queries.GetPaged
{
    public class GetPagedUserQuery : DataTableRequest, IRequest<DataTableResponse<UserResponse>>
    {
        public GetPagedUserQuery(DataTableRequest request)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
            this.SearchColumn = request.SearchColumn;
            this.Filters = request.Filters;
            this.StartDate = request.StartDate;
            this.EndDate = request.EndDate;
            this.SelectedIds = request.SelectedIds;
        }
    }

    internal class GetPagedUserQueryHandler : IRequestHandler<GetPagedUserQuery, DataTableResponse<UserResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedUserQueryHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;

        public GetPagedUserQueryHandler(
            IDataTableService dataTableService, 
            IUnitOfWork<Guid> unitOfWork, 
            UserManager<ApplicationUser> userManager, 
            ILogger<GetPagedUserQueryHandler> logger, 
            ICurrentUserService currentUserService,
            IOpenIddictApplicationManager applicationManager,
            IOpenIddictAuthorizationManager authorizationManager)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
            _currentUserService = currentUserService;
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
        }

        public async Task<DataTableResponse<UserResponse>> Handle(GetPagedUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _userManager.Users.AsNoTracking();

                if (!_currentUserService.IsMasterTenant)
                {
                    var tenantId = _currentUserService.TenantId;
                    query = query.Where(u => u.TenantId == tenantId);
                }

                var result = await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new UserResponse
                            {
                                Id = e.Id,
                                UserName = e.UserName,
                                Email = e.Email,
                                FirstName = e.Name,
                                LastName = string.Empty,
                                PhoneNumber = e.PhoneNumber,
                                IsEmailConfirmed = e.EmailConfirmed,
                                TenantId = e.TenantId,
                                CreatedAt = e.CreatedOn ?? DateTime.MinValue,
                                UpdatedAt = e.LastModifiedOn ?? DateTime.MinValue,
                                IsActive = e.IsActive,
                                IsLockedOut = e.LockoutEnd.HasValue && e.LockoutEnd.Value > DateTimeOffset.UtcNow,
                                LockoutEnd = e.LockoutEnd,
                                CreatedBy = e.CreatedBy,
                                CreatedOn = e.CreatedOn,
                                LastModifiedBy = e.LastModifiedBy,
                                LastModifiedOn = e.LastModifiedOn,
                                IPAddress = e.IPAddress,
                                IsDeleted = e.IsDeleted
                            },
                            e => true,
                            new List<string>
                            {
                                nameof(Domain.Entities.ApplicationUser.UserName),
                                nameof(Domain.Entities.ApplicationUser.Email),
                                nameof(Domain.Entities.ApplicationUser.Name),
                                nameof(Domain.Entities.ApplicationUser.CreatedBy),
                                nameof(Domain.Entities.ApplicationUser.CreatedOn),
                                nameof(Domain.Entities.ApplicationUser.LastModifiedBy),
                                nameof(Domain.Entities.ApplicationUser.LastModifiedOn),
                                nameof(Domain.Entities.ApplicationUser.IPAddress),
                                nameof(Domain.Entities.ApplicationUser.IsDeleted)
                            },
                            cancellationToken);

                if (result.Data != null && result.Data.Any())
                {
                    // 1. Fetch Tenant Names
                    var tenantIds = result.Data.Select(u => u.TenantId).Distinct().ToList();
                    var tenants = await _unitOfWork.Repository<Domain.Entities.Tenants>().Entities
                        .Where(t => tenantIds.Contains(t.Id))
                        .Select(t => new { t.Id, t.Name })
                        .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

                    // 2. Fetch all Roles and Clients/Applications for these users
                    foreach (var userDto in result.Data)
                    {
                        // Set Tenant Name
                        if (tenants.TryGetValue(userDto.TenantId, out var tenantName))
                        {
                            userDto.TenantName = tenantName;
                        }
                        else
                        {
                            userDto.TenantName = "Global";
                        }

                        // Set Roles
                        var appUser = await _userManager.FindByIdAsync(userDto.Id.ToString());
                        if (appUser != null)
                        {
                            var roles = await _userManager.GetRolesAsync(appUser);
                            userDto.Roles = roles.ToList();
                            
                            // Find OpenIddict permanent authorizations for this user
                            var authorizations = _authorizationManager.FindAsync(
                                subject: appUser.Id.ToString(),
                                client: null,
                                status: OpenIddictConstants.Statuses.Valid,
                                type: OpenIddictConstants.AuthorizationTypes.Permanent,
                                scopes: null);

                            await foreach (var auth in authorizations)
                            {
                                var appIdString = await _authorizationManager.GetApplicationIdAsync(auth);
                                if (Guid.TryParse(appIdString, out Guid appId))
                                {
                                    userDto.ClientIds.Add(appId);
                                }
                            }
                        }
                    }

                    // 3. Look up Client display names for all fetched ClientIds
                    var allClientIds = result.Data.SelectMany(u => u.ClientIds).Distinct().ToList();
                    if (allClientIds.Any())
                    {
                        var clientNamesMap = new Dictionary<Guid, string>();
                        foreach (var clientId in allClientIds)
                        {
                            var clientObj = await _applicationManager.FindByIdAsync(clientId.ToString(), cancellationToken);
                            if (clientObj is Domain.Entities.ApplicationClient clientApp)
                            {
                                clientNamesMap[clientId] = clientApp.DisplayName ?? clientApp.ClientId ?? clientId.ToString();
                            }
                        }

                        foreach (var userDto in result.Data)
                        {
                            userDto.ClientNames = userDto.ClientIds
                                .Select(cid => clientNamesMap.TryGetValue(cid, out var name) ? name : cid.ToString())
                                .ToList();
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting paged users.");
                return new DataTableResponse<UserResponse>
                {
                    Data = new List<UserResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
