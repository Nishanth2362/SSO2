using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SSO.Application.Helper;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Requests.Features;
using SSO.Application.Responses.Features;
using SSO.Common.Constants.Application;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Infrastructure.Services.Features
{
    public class ScopeServices : IScopeServices
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly IExcelService _excelService;
        private readonly IDapperRepository _dapper;
        private readonly ILogger<ScopeServices> _logger;

        public ScopeServices(ApplicationDbContext dbContext, IOpenIddictScopeManager scopeManager, IExcelService excelService,
            IDapperRepository dapper, ILogger<ScopeServices> logger)
        {
            _dbContext = dbContext;
            _scopeManager = scopeManager;
            _excelService = excelService;
            _dapper = dapper;
            _logger = logger;
        }

        public async Task<IResult<ImportScopesResponse>> ImportScopesAsync(Stream data, Guid? clientId = null)
        {
            var mappers = new Dictionary<string, Func<DataRow, ScopeRequest, object>>
            {
                { "Scope Name", (row, req) => req.Name = GetValue(row, "Scope Name") },
                { "Display Name", (row, req) => req.DisplayName = GetValue(row, "Display Name") },
                { "Permission Code", (row, req) => {
                    var code = GetValue(row, "Permission Code");
                    if (!string.IsNullOrEmpty(code)) {
                        req.Permissions.Add(new Permissions {
                            Code = code,
                            Description = GetValue(row, "Permission Description")
                        });
                    }
                    return null;
                }},
                { "Permission Description", (row, req) => null }
            };

            var result = await _excelService.ImportAsync(data, mappers);
            if (!result.Succeeded) return await Result<ImportScopesResponse>.FailAsync(result.Messages);

            var scopeRequests = result.Data;

            // Group by Scope Name — Excel may have multiple rows for the same scope (one per permission)
            var groupedScopes = scopeRequests
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new ScopeRequest
                {
                    Name = g.Key,
                    DisplayName = g.First().DisplayName?.Trim() ?? string.Empty,
                    Permissions = g.SelectMany(x => x.Permissions)
                                   .Where(p => !string.IsNullOrWhiteSpace(p.Code))
                                   .GroupBy(p => p.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                                   .Select(pg => pg.First())
                                   .ToList()
                })
                .ToList();

            // Fetch existing permission codes BEFORE starting transaction/modifications to detect pre-existing duplicates
            var preExistingPermissionCodes = await _dbContext.Permissions
                .Where(p => p.ClientApplicationId == clientId)
                .Select(p => p.Code)
                .ToListAsync();

            var response = new ImportScopesResponse();

            // Wrap everything in a transaction so a mid-flight failure cannot leave
            // partially-committed state that causes ASP.NET to throw
            // "StatusCode cannot be set because the response has already started".
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                int importedCount = 0;
                var processedScopeIds = new List<Guid>();
                foreach (var req in groupedScopes)
                {
                    var id = await CreateScopeAsync(req, clientId, response.SkippedPermissions, preExistingPermissionCodes);
                    processedScopeIds.Add(id);
                    importedCount++;
                }

                // Sync: Remove client-scope mappings NOT present in the Excel file.
                // We use a !Any (NOT EXISTS) subquery which is more robust in MySQL than a NOT IN clause.
                if (clientId.HasValue)
                {
                    var scopeNamesInExcel = groupedScopes.Select(x => x.Name).ToList();

                    var validScopeIds = await _dbContext.Scopes
                        .Where(s => scopeNamesInExcel.Contains(s.Name))
                        .Select(s => s.Id)
                        .ToListAsync();

                    var staleClientMappings = await _dbContext.ApplicationClientScopes
                        .Where(acs => acs.ClientId == clientId.Value && !validScopeIds.Contains(acs.ScopeId))
                        .ToListAsync();

                    if (staleClientMappings.Any())
                    {
                        _dbContext.ApplicationClientScopes.RemoveRange(staleClientMappings);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                response.ImportedCount = importedCount;

                await transaction.CommitAsync();
                return await Result<ImportScopesResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return await Result<ImportScopesResponse>.FailAsync($"Import failed: {ex.Message}");
            }
        }

        public async Task<Guid> CreateScopeAsync(ScopeRequest req, Guid? clientId = null, List<string>? skippedPermissions = null, List<string>? preExistingPermissionCodes = null)
        {
            try
            {
                if (req == null)
                    throw new ArgumentNullException(nameof(req));

                req.Name = req.Name?.Trim() ?? string.Empty;
                req.DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? req.Name : req.DisplayName.Trim();
                req.Permissions ??= new List<Permissions>();

                if (string.IsNullOrWhiteSpace(req.Name))
                    throw new InvalidOperationException("Scope name is required.");

                var scope = await _scopeManager.FindByNameAsync(req.Name);
                string? clientAppId = null;
                if (clientId.HasValue)
                {
                    var client = await _dbContext.Clients.FindAsync(clientId.Value);
                    // Only add confidential clients (Web/Machine) as resources
                    if (client != null && client.AppClientType.RequiresClientSecret())
                        clientAppId = client.ClientId;
                }

                if (scope == null)
                {
                    var descriptor = new OpenIddictScopeDescriptor
                    {
                        Name = req.Name,
                        DisplayName = req.DisplayName
                    };
                    descriptor.Resources.Add(req.Name);
                    if (!string.IsNullOrEmpty(clientAppId))
                        descriptor.Resources.Add(clientAppId);

                    await _scopeManager.CreateAsync(descriptor);
                    scope = await _scopeManager.FindByNameAsync(req.Name);
                }
                else
                {
                    var descriptor = new OpenIddictScopeDescriptor();
                    await _scopeManager.PopulateAsync(descriptor, scope);

                    var changed = false;
                    if (descriptor.DisplayName != req.DisplayName) { descriptor.DisplayName = req.DisplayName; changed = true; }
                    if (!descriptor.Resources.Contains(req.Name)) { descriptor.Resources.Add(req.Name); changed = true; }
                    if (!string.IsNullOrEmpty(clientAppId) && !descriptor.Resources.Contains(clientAppId)) { descriptor.Resources.Add(clientAppId); changed = true; }

                    if (changed)
                        await _scopeManager.UpdateAsync(scope, descriptor);
                }

                var rawId = await _scopeManager.GetIdAsync(scope);
                if (string.IsNullOrEmpty(rawId))
                    throw new Exception("Failed to retrieve ID for scope.");

                var scopeId = Guid.Parse(rawId);

                // ── Client → Scope mapping ──────────────────────────────────────────────
                // IMPORTANT: AnyAsync only queries the DB, not the EF change tracker.
                // If a previous scope-iteration already Added the same (ClientId, ScopeId)
                // key but it hasn't been committed yet, EF will throw a duplicate-tracking
                // exception. We therefore check the tracker FIRST, then the DB.
                // After saving, we Detach the entity so the tracker is clean for the next iteration.
                if (clientId.HasValue)
                {
                    var trackedClientScope = _dbContext.ChangeTracker
                        .Entries<ApplicationClientScope>()
                        .Any(e => e.Entity.ClientId == clientId.Value && e.Entity.ScopeId == scopeId);

                    if (!trackedClientScope &&
                        !await _dbContext.ApplicationClientScopes
                            .AnyAsync(x => x.ClientId == clientId.Value && x.ScopeId == scopeId))
                    {
                        var clientScopeEntry = _dbContext.ApplicationClientScopes.Add(new ApplicationClientScope
                        {
                            Id = Guid.NewGuid(),
                            ClientId = clientId.Value,
                            ScopeId = scopeId
                        });
                        // Flush and detach immediately — prevents duplicate-key tracking on the next
                        // scope's iteration before the outer transaction is committed.
                        await _dbContext.SaveChangesAsync();
                        clientScopeEntry.State = EntityState.Detached;
                    }
                }

                // ── Permission → Scope mappings ─────────────────────────────────────────
                var normalizedPermissions = req.Permissions
                    .Where(p => !string.IsNullOrWhiteSpace(p.Code))
                    .GroupBy(p => p.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new Permissions
                    {
                        Code = g.Key,
                        Description = string.IsNullOrWhiteSpace(g.Last().Description) ? g.Key : g.Last().Description.Trim()
                    })
                    .ToList();
                var currentPermissionIds = new List<Guid>();

                foreach (var permissionRequest in normalizedPermissions)
                {
                    var permission = await _dbContext.Permissions
                        .FirstOrDefaultAsync(x => x.Code == permissionRequest.Code
                                               && x.ClientApplicationId == clientId);

                    if (permission == null)
                    {
                        permission = new Permission
                        {
                            Id = Guid.NewGuid(),
                            Code = permissionRequest.Code,
                            Description = permissionRequest.Description,
                            ClientApplicationId = clientId
                        };
                        _dbContext.Permissions.Add(permission);
                    }
                    else
                    {
                        // Check if it already existed in the database BEFORE this import process started
                        if (preExistingPermissionCodes != null && preExistingPermissionCodes.Contains(permissionRequest.Code, StringComparer.OrdinalIgnoreCase))
                        {
                            // Avoid modifying / overwriting the existing permission description
                            if (skippedPermissions != null && !skippedPermissions.Contains(permissionRequest.Code))
                            {
                                skippedPermissions.Add(permissionRequest.Code);
                            }
                        }
                        else
                        {
                            // Otherwise, update description (since it's a new duplicate in the same import file or modified in a non-duplicate context)
                            permission.Description = permissionRequest.Description;
                        }
                    }

                    // Save here so permission.Id is populated (DB-generated) before use below.
                    await _dbContext.SaveChangesAsync();
                    currentPermissionIds.Add(permission.Id);

                    // Same tracker-first guard for ScopePermission join rows.
                    var trackedScopePermission = _dbContext.ChangeTracker
                        .Entries<ApplicationScopePermission>()
                        .Any(e => e.Entity.ScopeId == scopeId && e.Entity.PermissionId == permission.Id);

                    if (!trackedScopePermission &&
                        !await _dbContext.ApplicationScopePermissions
                            .AnyAsync(x => x.ScopeId == scopeId && x.PermissionId == permission.Id))
                    {
                        var mappingEntry = _dbContext.ApplicationScopePermissions.Add(new ApplicationScopePermission
                        {
                            Id = Guid.NewGuid(),
                            ScopeId = scopeId,
                            PermissionId = permission.Id
                        });
                        await _dbContext.SaveChangesAsync();
                        mappingEntry.State = EntityState.Detached;
                    }
                }

                await DeleteStaleScopePermissionsAsync(scopeId, clientId, currentPermissionIds);

                return scopeId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception($"Error creating/updating scope '{req.Name}': {ex.Message}", ex);
            }
        }

        private static string GetValue(DataRow row, string columnName)
        {
            var col = row.Table.Columns.Cast<DataColumn>().FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
            return col != null ? row[col]?.ToString() ?? string.Empty : string.Empty;
        }

        private async Task DeleteStaleScopePermissionsAsync(Guid scopeId, Guid? clientId, IReadOnlyCollection<Guid> currentPermissionIds)
        {
            var clientPermissionIds = await _dbContext.Permissions
                .Where(p => p.ClientApplicationId == clientId)
                .Select(p => p.Id)
                .ToListAsync();

            var staleMappings = await _dbContext.ApplicationScopePermissions
                .Where(sp => sp.ScopeId == scopeId
                          && clientPermissionIds.Contains(sp.PermissionId)
                          && !currentPermissionIds.Contains(sp.PermissionId))
                .ToListAsync();

            if (staleMappings.Any())
            {
                _dbContext.ApplicationScopePermissions.RemoveRange(staleMappings);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
