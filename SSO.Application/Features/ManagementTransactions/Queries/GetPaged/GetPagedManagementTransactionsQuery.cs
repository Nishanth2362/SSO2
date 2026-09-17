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
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Queries.GetPaged
{
    public class GetPagedManagementTransactionsQuery : DataTableRequest, IRequest<DataTableResponse<ManagementTransactionResponse>>
    {
        public GetPagedManagementTransactionsQuery(DataTableRequest request)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
            this.SearchColumn = request.SearchColumn;
            this.Filters = request.Filters ?? new Dictionary<string, string>();
            this.StartDate = request.StartDate;
            this.EndDate = request.EndDate;
            this.SelectedIds = request.SelectedIds;
        }
    }

    internal class GetPagedManagementTransactionsQueryHandler : IRequestHandler<GetPagedManagementTransactionsQuery, DataTableResponse<ManagementTransactionResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly IDataTableService _dataTableService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeService _dateTimeService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly ILogger<GetPagedManagementTransactionsQueryHandler> _logger;

        public GetPagedManagementTransactionsQueryHandler(
            IUnitOfWork<Guid> unitOfWork,
            IDataTableService dataTableService,
            ICurrentUserService currentUserService,
            IDateTimeService dateTimeService,
            UserManager<ApplicationUser> userManager,
            IOpenIddictApplicationManager applicationManager,
            ILogger<GetPagedManagementTransactionsQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _dataTableService = dataTableService;
            _currentUserService = currentUserService;
            _dateTimeService = dateTimeService;
            _userManager = userManager;
            _applicationManager = applicationManager;
            _logger = logger;
        }

        public async Task<DataTableResponse<ManagementTransactionResponse>> Handle(GetPagedManagementTransactionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<ManagementTransaction>().Entities.AsNoTracking();

                // Multi-tenancy isolation
                if (!_currentUserService.IsMasterTenant)
                {
                    query = query.Where(x => x.TenantId == _currentUserService.TenantId);
                }

                // Handle custom filters
                if (request.Filters != null)
                {
                    if (request.Filters.TryGetValue("Status", out var statusVal) && !string.IsNullOrEmpty(statusVal) && Enum.TryParse<ManagementTransactionStatus>(statusVal, true, out var statusEnum))
                    {
                        query = query.Where(x => x.Status == statusEnum);
                        request.Filters.Remove("Status");
                    }

                    if (request.Filters.TryGetValue("Scope", out var scopeVal) && !string.IsNullOrEmpty(scopeVal))
                    {
                        query = query.Where(x => x.Scope == scopeVal.Trim().ToLower());
                        request.Filters.Remove("Scope");
                    }

                    if (request.Filters.TryGetValue("ClientId", out var clientIdVal) && !string.IsNullOrEmpty(clientIdVal))
                    {
                        query = query.Where(x => x.ClientId == clientIdVal.Trim());
                        request.Filters.Remove("ClientId");
                    }

                    if (request.Filters.TryGetValue("TenantId", out var tenantIdVal) && Guid.TryParse(tenantIdVal, out var tid))
                    {
                        query = query.Where(x => x.TenantId == tid);
                        request.Filters.Remove("TenantId");
                    }
                }

                // Normalize sort column property name to PascalCase for EF.Property reflection
                if (!string.IsNullOrEmpty(request.SortColumn))
                {
                    if (string.Equals(request.SortColumn, "createdOn", StringComparison.OrdinalIgnoreCase)) request.SortColumn = nameof(ManagementTransaction.CreatedOn);
                    else if (string.Equals(request.SortColumn, "expiresOn", StringComparison.OrdinalIgnoreCase)) request.SortColumn = nameof(ManagementTransaction.ExpiresOn);
                    else if (string.Equals(request.SortColumn, "scope", StringComparison.OrdinalIgnoreCase)) request.SortColumn = nameof(ManagementTransaction.Scope);
                    else if (string.Equals(request.SortColumn, "clientId", StringComparison.OrdinalIgnoreCase) || string.Equals(request.SortColumn, "clientDisplayName", StringComparison.OrdinalIgnoreCase)) request.SortColumn = nameof(ManagementTransaction.ClientId);
                    else if (string.Equals(request.SortColumn, "status", StringComparison.OrdinalIgnoreCase)) request.SortColumn = nameof(ManagementTransaction.Status);
                }
                else
                {
                    request.SortColumn = nameof(ManagementTransaction.CreatedOn);
                    request.SortDirection = "desc";
                }

                var result = await _dataTableService.BuildAsync(
                    query,
                    request,
                    e => new ManagementTransactionResponse
                    {
                        Id = e.Id,
                        ClientId = e.ClientId,
                        ClientDisplayName = e.ClientId,
                        TenantId = e.TenantId,
                        TenantName = null,
                        UserId = e.UserId,
                        UserName = null,
                        UserEmail = null,
                        Scope = e.Scope,
                        TargetUrl = e.TargetUrl,
                        CallbackUrl = e.CallbackUrl,
                        State = e.State,
                        ExpiresOn = e.ExpiresOn,
                        IsConsumed = e.IsConsumed,
                        ConsumedOn = e.ConsumedOn,
                        ConsumedIpAddress = e.ConsumedIpAddress,
                        ConsumedUserAgent = e.ConsumedUserAgent,
                        HasResultCode = !string.IsNullOrEmpty(e.ResultCodeHash),
                        IsResultCodeConsumed = e.IsResultCodeConsumed,
                        ResultCodeConsumedOn = e.ResultCodeConsumedOn,
                        Status = e.Status,
                        RevokedReason = e.RevokedReason,
                        RevokedOn = e.RevokedOn,
                        RevokedBy = e.RevokedBy,
                        CreatedOn = e.CreatedOn,
                        CreatedBy = e.CreatedBy,
                        ChangedFieldsJson = e.ChangedFieldsJson
                    },
                    e => true,
                    new List<string>
                    {
                        nameof(ManagementTransaction.ClientId),
                        nameof(ManagementTransaction.Scope),
                        nameof(ManagementTransaction.TargetUrl),
                        nameof(ManagementTransaction.CallbackUrl),
                        nameof(ManagementTransaction.ConsumedIpAddress),
                        nameof(ManagementTransaction.CreatedOn),
                        nameof(ManagementTransaction.ExpiresOn),
                        nameof(ManagementTransaction.Status)
                    },
                    cancellationToken
                );

                // Post-process the paged records in memory for User, Client, and Tenant names
                if (result.Data != null && result.Data.Any())
                {
                    var tenantIds = result.Data.Select(x => x.TenantId).Distinct().ToList();
                    var tenants = await _unitOfWork.Repository<Domain.Entities.Tenants>().Entities
                        .AsNoTracking()
                        .Where(t => tenantIds.Contains(t.Id))
                        .Select(t => new { t.Id, t.Name })
                        .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

                    var userIds = result.Data.Select(x => x.UserId).Distinct().ToList();
                    var users = await _userManager.Users
                        .AsNoTracking()
                        .Where(u => userIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.UserName, u.Email, u.Name })
                        .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

                    foreach (var item in result.Data)
                    {
                        if (tenants.TryGetValue(item.TenantId, out var tName))
                        {
                            item.TenantName = tName;
                        }

                        if (users.TryGetValue(item.UserId, out var uObj))
                        {
                            item.UserName = !string.IsNullOrWhiteSpace(uObj.Name) ? uObj.Name : uObj.UserName;
                            item.UserEmail = uObj.Email;
                        }

                        if (!string.IsNullOrEmpty(item.ClientId))
                        {
                            var app = await _applicationManager.FindByClientIdAsync(item.ClientId, cancellationToken);
                            if (app != null)
                            {
                                var displayName = await _applicationManager.GetDisplayNameAsync(app, cancellationToken);
                                if (!string.IsNullOrWhiteSpace(displayName))
                                {
                                    item.ClientDisplayName = displayName;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(item.ChangedFieldsJson))
                        {
                            try
                            {
                                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(item.ChangedFieldsJson);
                                if (parsed != null) item.Changes = parsed;
                            }
                            catch
                            {
                                // Ignore json parsing error
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching paged management transactions.");
                return new DataTableResponse<ManagementTransactionResponse>
                {
                    Draw = request.Draw,
                    RecordsTotal = 0,
                    RecordsFiltered = 0,
                    Data = new List<ManagementTransactionResponse>()
                };
            }
        }
    }
}
