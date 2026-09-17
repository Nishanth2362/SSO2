using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        }
    }

    internal class GetPagedUserQueryHandler : IRequestHandler<GetPagedUserQuery, DataTableResponse<UserResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedUserQueryHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        public GetPagedUserQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, UserManager<ApplicationUser> userManager, ILogger<GetPagedUserQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<DataTableResponse<UserResponse>> Handle(GetPagedUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _userManager.Users.AsNoTracking();
                return await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new UserResponse
                            {
                                Id = e.Id,
                                UserName = e.UserName,
                                Email = e.Email,
                                FirstName = e.Name,
                                LastName = string.Empty,
                                TenantId = e.TenantId,
                                CreatedAt = e.CreatedOn ?? DateTime.MinValue,
                                UpdatedAt = e.LastModifiedOn ?? DateTime.MinValue,
                                IsActive = e.IsActive
                            },
                            e => true,
                            new List<string>
                            {
                                nameof(Domain.Entities.ApplicationUser.UserName),
                                nameof(Domain.Entities.ApplicationUser.Email),
                                nameof(Domain.Entities.ApplicationUser.Name)
                            },
                            cancellationToken);
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
