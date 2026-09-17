using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.RolePermissions.Queries.GetPaged
{
    public class GetPagedRolePermissionsQuery : DataTableRequest, IRequest<DataTableResponse<RolePermissionResponse>>
    {
        public Guid RoleId { get; set; }
        public GetPagedRolePermissionsQuery(DataTableRequest request, Guid roleId)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
            this.RoleId = roleId;
        }
    }

    internal class GetPagedRolePermissionsQueryHandler : IRequestHandler<GetPagedRolePermissionsQuery, DataTableResponse<RolePermissionResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedRolePermissionsQueryHandler> _logger;

        public GetPagedRolePermissionsQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedRolePermissionsQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DataTableResponse<RolePermissionResponse>> Handle(GetPagedRolePermissionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.RolePermission>().Entities
                    .Where(x => x.RoleId == request.RoleId)
                    .Include(x => x.Role)
                    .Include(x => x.Permission)
                    .Include(x => x.ApplicationClient)
                    .AsNoTracking();

                return await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new RolePermissionResponse
                            {
                                Id = e.Id,
                                RoleId = e.RoleId,
                                RoleName = e.Role.Name!,
                                PermissionId = e.PermissionId,
                                PermissionName = e.Permission.Code,
                                ClientApplicationId = e.ApplicationClientId,
                                ClientApplicationName = e.ApplicationClient.DisplayName ?? string.Empty
                            },
                            e => true,
                            new List<string>
                            {
                                "Permission.Code",
                                "ApplicationClient.DisplayName"
                            },
                            cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting paged role permissions.");
                return new DataTableResponse<RolePermissionResponse>
                {
                    Data = new List<RolePermissionResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
