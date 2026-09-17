using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Roles.Queries.GetPaged
{
    public class GetPagedRolesQuery : DataTableRequest, IRequest<DataTableResponse<RoleResponse>>
    {
        public Guid TenantId { get; set; } = Guid.Empty;
        public GetPagedRolesQuery(DataTableRequest request)
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
        }
    }

    internal class GetPagedRolesQueryHandler : IRequestHandler<GetPagedRolesQuery, DataTableResponse<RoleResponse>>
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IDataTableService _dataTableService;
        private readonly ILogger<GetPagedRolesQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public GetPagedRolesQueryHandler(RoleManager<ApplicationRole> roleManager, IDataTableService dataTableService, ILogger<GetPagedRolesQueryHandler> logger, ICurrentUserService currentUserService)
        {
            _roleManager = roleManager;
            _dataTableService = dataTableService;
            _logger = logger;
            _currentUserService = currentUserService;
        }
        public async Task<DataTableResponse<RoleResponse>> Handle(GetPagedRolesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _roleManager.Roles;

                if (!_currentUserService.IsMasterTenant)
                {
                    var tenantId = _currentUserService.TenantId;
                    query = query.Where(x => x.TenantId == tenantId);
                }
                else if (request.TenantId != Guid.Empty)
                {
                    query = query.Where(x => x.TenantId == request.TenantId);
                }

                return await _dataTableService.BuildAsync(
                                query,
                                request,
                                e => new RoleResponse
                                {
                                    Id = e.Id,
                                    Name = e.Name,
                                    Description = e.Description,
                                    TenantId = e.TenantId,
                                    IsSystemRole = e.IsSystemRole,
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
                                    nameof(Domain.Entities.ApplicationRole.Name),
                                    nameof(Domain.Entities.ApplicationRole.Description),
                                    nameof(Domain.Entities.ApplicationRole.CreatedBy),
                                    nameof(Domain.Entities.ApplicationRole.CreatedOn),
                                    nameof(Domain.Entities.ApplicationRole.LastModifiedBy),
                                    nameof(Domain.Entities.ApplicationRole.LastModifiedOn),
                                    nameof(Domain.Entities.ApplicationRole.IPAddress),
                                    nameof(Domain.Entities.ApplicationRole.IsDeleted)
                                },
                                cancellationToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting paged users.");
                return new DataTableResponse<RoleResponse>
                {
                    Data = new List<RoleResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
