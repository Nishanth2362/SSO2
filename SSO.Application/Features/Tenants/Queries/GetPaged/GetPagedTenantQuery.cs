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
using System.Text;

namespace SSO.Application.Features.Tenants.Queries.GetPaged
{
    public class GetPagedTenantQuery : DataTableRequest, IRequest<DataTableResponse<TenantResponse>>
    {
        public GetPagedTenantQuery(DataTableRequest request)
        {
            this.SearchValue= request.SearchValue;
            this.Start= request.Start;
            this.Length= request.Length;
            this.Draw= request.Draw;
            this.SortColumn= request.SortColumn;
            this.SortDirection= request.SortDirection;
            this.SearchColumn = request.SearchColumn;
            this.Filters = request.Filters;
            this.StartDate = request.StartDate;
            this.EndDate = request.EndDate;
            this.SelectedIds = request.SelectedIds;
        }
    }
    internal class GetPagedTenantQueryHandler : IRequestHandler<GetPagedTenantQuery, DataTableResponse<TenantResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<GetPagedTenantQueryHandler> _logger;

        public GetPagedTenantQueryHandler(
            IDataTableService dataTableService,
            IUnitOfWork<Guid> unitOfWork,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<GetPagedTenantQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<DataTableResponse<TenantResponse>> Handle(GetPagedTenantQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.Tenants>().Entities
                    .Include(x => x.TenantSubscriptions)
                        .ThenInclude(ts => ts.Subscriptions)
                    .Include(x => x.TenantClients)
                    .AsNoTracking();

                if (request.Filters != null && request.Filters.TryGetValue("SubscriptionId", out var subscriptionIdVal) && !string.IsNullOrEmpty(subscriptionIdVal))
                {
                    var subscriptionIds = subscriptionIdVal.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty)
                        .Where(g => g != Guid.Empty)
                        .ToList();

                    if (subscriptionIds.Any())
                    {
                        query = query.Where(t => t.TenantSubscriptions.Any(ts => ts.IsActive && subscriptionIds.Contains(ts.SubscriptionId)));
                    }

                    // Remove from Filters so that DataTableService doesn't fail on reflection lookup
                    request.Filters.Remove("SubscriptionId");
                }

                var result = await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new TenantResponse
                            {
                                Id = e.Id,
                                Name = e.Name,
                                DatabaseMode = e.DatabaseMode,
                                Code = e.Code,
                                IsActive = e.IsActive,
                                DatabaseProvider = e.DatabaseProvider,
                                DatabaseName = e.DatabaseName,
                                ConnectionString = null,
                                GracePeriodDays = e.GracePeriodDays,
                                Currency = e.Currency,
                                Email = e.Email,
                                Phone = e.Phone,
                                Website = e.Website,
                                BillingAddress = e.BillingAddress,
                                FaviconUrl = e.FaviconUrl,
                                BackgroundText = e.BackgroundText,
                                SubscriptionName = e.TenantSubscriptions.Any(x => x.IsActive) ? e.TenantSubscriptions.First(x => x.IsActive).Subscriptions.Name : "No Active Plan",
                                SubscriptionExpiry = e.TenantSubscriptions.Any(x => x.IsActive) ? e.TenantSubscriptions.First(x => x.IsActive).EndDateUtc : (DateTime?)null,
                                LogoUrl = e.LogoUrl,
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
                                nameof(Domain.Entities.Tenants.Name),
                                nameof(Domain.Entities.Tenants.Code),
                                nameof(Domain.Entities.Tenants.DatabaseMode),
                                nameof(Domain.Entities.Tenants.GracePeriodDays),
                                nameof(Domain.Entities.Tenants.IsActive),
                                nameof(Domain.Entities.Tenants.DatabaseProvider),
                                nameof(Domain.Entities.Tenants.DatabaseName),
                                nameof(Domain.Entities.Tenants.Email),
                                nameof(Domain.Entities.Tenants.Phone),
                                nameof(Domain.Entities.Tenants.Website),
                                nameof(Domain.Entities.Tenants.BillingAddress),
                                nameof(Domain.Entities.Tenants.Currency),
                                nameof(Domain.Entities.Tenants.CreatedBy),
                                nameof(Domain.Entities.Tenants.CreatedOn),
                                nameof(Domain.Entities.Tenants.LastModifiedBy),
                                nameof(Domain.Entities.Tenants.LastModifiedOn),
                                nameof(Domain.Entities.Tenants.IPAddress),
                                nameof(Domain.Entities.Tenants.IsDeleted)
                            },
                            cancellationToken);

                if (result?.Data != null && result.Data.Any())
                {
                    var tenantIds = result.Data.Select(t => t.Id).ToList();

                    var appCounts = await _unitOfWork.Repository<TenantClient>().Entities
                        .Where(tc => tenantIds.Contains(tc.TenantId))
                        .GroupBy(tc => tc.TenantId)
                        .Select(g => new { TenantId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

                    var userCounts = await _userManager.Users
                        .Where(u => tenantIds.Contains(u.TenantId) && !u.IsDeleted)
                        .GroupBy(u => u.TenantId)
                        .Select(g => new { TenantId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

                    var roleCounts = await _roleManager.Roles
                        .Where(r => tenantIds.Contains(r.TenantId) && !r.IsDeleted)
                        .GroupBy(r => r.TenantId)
                        .Select(g => new { TenantId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

                    foreach (var tenant in result.Data)
                    {
                        tenant.ApplicationsCount = appCounts.TryGetValue(tenant.Id, out var ac) ? ac : 0;
                        tenant.UsersCount = userCounts.TryGetValue(tenant.Id, out var uc) ? uc : 0;
                        tenant.RolesCount = roleCounts.TryGetValue(tenant.Id, out var rc) ? rc : 0;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new DataTableResponse<TenantResponse>
                {
                    Data = new List<TenantResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }

        }
    }
}
