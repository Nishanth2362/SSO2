using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using SSO.Application.Helper;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Requests.DataTable;
using SSO.Application.Requests.Features;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Infrastructure.Contexts;
using SSO.Infrastructure.Models;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SSO.Infrastructure.Services.Features
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly IOpenIddictApplicationManager _appManager;
        private readonly ILogger<ClientService> _logger;
        private readonly IDataTableService _dataTableService;
        private readonly IScopeServices _scopeServices;
        private readonly DefaultSetting _defaultSetting;
        private readonly IEnumerable<IRequestValidator<ClientRequest>> _validators;
        private readonly IMemoryCache _memoryCache;
        public ClientService(IUnitOfWork<Guid> unitOfWork, IOpenIddictApplicationManager appManager,
            IDataTableService dataTableService, ApplicationDbContext dbContext, IScopeServices scopeServices,
             IConfiguration configuration, ILogger<ClientService> logger,
             IEnumerable<IRequestValidator<ClientRequest>> validators,
             IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _appManager = appManager;
            _scopeServices = scopeServices;
            _logger = logger;
            _dataTableService = dataTableService;
            _defaultSetting = configuration.GetSection("DefaultSetting").Get<DefaultSetting>()!;
            _validators = validators;
            _memoryCache = memoryCache;
        }
        public async Task<Result<Guid>> CreateAsync(ClientRequest request)
        {
            try
            {
                if (request == null)
                    return await Result<Guid>.FailAsync("Client request payload is required.");

                if (_validators.Any())
                {
                    var failures = new List<ValidationError>();
                    foreach (var validator in _validators)
                    {
                        var validationResult = await validator.ValidateAsync(request, CancellationToken.None);
                        failures.AddRange(validationResult);
                    }

                    if (failures.Count > 0)
                        return await Result<Guid>.FailAsync(failures.Select(x => x.ErrorMessage).Distinct().ToList());
                }

                if (string.IsNullOrWhiteSpace(request.ClientId))
                    return await Result<Guid>.FailAsync("Client ID is required.");

                if (string.IsNullOrWhiteSpace(request.ClientName))
                    return await Result<Guid>.FailAsync("Client name is required.");

                if (await _dbContext.Clients.AnyAsync(c => c.ClientId == request.ClientId && c.Id != request.Id))
                    return await Result<Guid>.FailAsync($"ClientId '{request.ClientId}' already exists.");

                request.RedirectUris ??= new List<string>();
                request.PostLogoutRedirectUris ??= new List<string>();
                request.Scopes ??= new List<ScopeRequest>();

                object app;
                var isEdit = request.Id.HasValue && request.Id.Value != Guid.Empty;
                if (request.Id.HasValue && request.Id.Value != Guid.Empty)
                {
                    app = await _appManager.FindByIdAsync(request.Id.Value.ToString());
                    if (app == null) return await Result<Guid>.FailAsync("Client not found.");
                }
                else
                {
                    app = null;
                }

                var isInteractiveClient = request.ClientType != ClientType.Machine;
                if (isInteractiveClient && request.RedirectUris.Count == 0)
                    return await Result<Guid>.FailAsync("At least one redirect URI is required for interactive applications.");

                var redirectUrisResult = ValidateUris(request.RedirectUris, "redirect URI");
                if (!redirectUrisResult.Succeeded)
                    return await Result<Guid>.FailAsync(redirectUrisResult.Messages);

                var postLogoutUrisResult = ValidateUris(request.PostLogoutRedirectUris, "post-logout redirect URI");
                if (!postLogoutUrisResult.Succeeded)
                    return await Result<Guid>.FailAsync(postLogoutUrisResult.Messages);

                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = request.ClientId,
                    DisplayName = request.ClientName,
                    ApplicationType = request.ClientType.ToApplicationType(),
                    ConsentType = request.ClientType.GetConsentType(),
                    ClientType = request.ClientType.GetClientType()
                };

                // Add resource-related permissions for confidential clients (required for Introspection)
                if (request.ClientType.RequiresClientSecret())
                {
                    // Add the client itself as a resource
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Resource + request.ClientId);

                }

                // Add configured scopes as permissions
                foreach (var scope in request.Scopes)
                {
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope.Name);
                    descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Resource + scope.Name);
                }

                // Redirect URIs
                if (request.RedirectUris != null)
                {
                    foreach (var uri in request.RedirectUris)
                    {
                        if (!string.IsNullOrWhiteSpace(uri))
                            descriptor.RedirectUris.Add(new Uri(uri));
                    }
                }

                if (request.PostLogoutRedirectUris != null)
                {
                    foreach (var uri in request.PostLogoutRedirectUris)
                    {
                        if (!string.IsNullOrWhiteSpace(uri))
                            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
                    }
                }

                // OAuth permissions
                descriptor.Permissions.UnionWith(OpenIdExtentions.BuildClientPermissions(request.ClientType));

                // Secret only for confidential clients
                if (request.ClientType.RequiresClientSecret())
                {
                    if (!string.IsNullOrWhiteSpace(request.ClientSecret))
                    {
                        descriptor.ClientSecret = request.ClientSecret;
                    }
                    else if (!isEdit)
                    {
                        descriptor.ClientSecret = GenerateClientSecret();
                    }
                }

                if (app != null)
                {
                    await _appManager.UpdateAsync(app, descriptor);
                }
                else
                {
                    app = await _appManager.CreateAsync(descriptor);
                }

                var entity = (ApplicationClient)app;

                // 🔥 SAVE TENANT + LICENSING fields into YOUR columns
                entity.RequireLicense = true;
                entity.IsActive = true;
                entity.Audience = request.Audience;
                entity.AppClientType = request.ClientType;
                entity.Require2FA = request.Require2FA;
                entity.AllowedLoginMethod = request.AllowedLoginMethod;
                entity.AllowPublicRegistration = request.AllowPublicRegistration;
                entity.DefaultRoleName = string.IsNullOrWhiteSpace(request.DefaultRoleName) ? "End User" : request.DefaultRoleName.Trim();

                var clientId = entity.Id;

                // 🔥 Update scopes (your mapping table) - Remove old mappings first
                var existingMappings = await _dbContext.ApplicationClientScopes
                    .Where(x => x.ClientId == clientId)
                    .ToListAsync();
                _dbContext.ApplicationClientScopes.RemoveRange(existingMappings);
                await _dbContext.SaveChangesAsync();
                // Add new mappings
                foreach (var scope in request.Scopes)
                {
                    var scopeId = await _scopeServices.CreateScopeAsync(scope, clientId); // Ensure scope exists
                    //_dbContext.ApplicationClientScopes.Add(new ApplicationClientScope
                    //{
                    //    Id = Guid.NewGuid(),
                    //    ClientId = clientId,
                    //    ScopeId = scopeId
                    //});
                }

                await _dbContext.SaveChangesAsync();

                return await Result<Guid>.SuccessAsync(clientId, isEdit ? $"Client {entity.ClientId} updated successfully" : $"Client {entity.ClientId} created successfully");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/editing client");
                return await Result<Guid>.FailAsync(ex.Message);
            }
        }

        private static Result<Guid> ValidateUris(List<string> uris, string label)
        {
            foreach (var uri in uris.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
                {
                    return Result<Guid>.Fail($"The value '{uri}' is not a valid absolute {label}.");
                }
            }

            return Result<Guid>.Success(Guid.Empty);
        }

        private static string GenerateClientSecret()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        }

        public async Task<Result<Guid>> DeleteAsync(Guid clientId)
        {
            try
            {
                var client = await _dbContext.Clients.FindAsync(clientId);
                if (client == null)
                    return await Result<Guid>.FailAsync("Client not found.");

                // Check for OpenIddict data if needed, but usually deleting the entity is enough if Cascades are set.
                // However, appManager might be better to use if it handles more internal stuff.
                // For now, I'll use appManager if I can find by Id.
                var openIdApp = await _appManager.FindByIdAsync(clientId.ToString());
                if (openIdApp != null)
                {
                    await _appManager.DeleteAsync(openIdApp);
                }
                else
                {
                    // Fallback to direct DB delete if appManager doesn't find it (rare if sync)
                    _dbContext.Clients.Remove(client);
                }

                await _dbContext.SaveChangesAsync();
                return await Result<Guid>.SuccessAsync(clientId, "Client deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client {ClientId}", clientId);
                return await Result<Guid>.FailAsync(ex.Message);
            }
        }

        public async Task<Result<List<ClientResponse>>> GetAllAsync(Guid tenantId)
        {
            try
            {
                var clients = await _dbContext.Clients
                    .Where(c => c.TenantClients.Any(tc => tc.TenantId == tenantId))
                    .Include(c => c.ClientScopes)
                        .ThenInclude(cs => cs.Scope)
                    .Select(e => new ClientResponse
                    {
                        Id = e.Id,
                        ClientId = e.ClientId,
                        ClientType = e.ClientType!,
                        IsActive = e.IsActive,
                        Audience = e.Audience,
                        Name = e.DisplayName,
                        Scopes = string.Join(",", e.ClientScopes.Select(x => x.Scope.Name).ToList()),
                        AppClientType = e.AppClientType.ToString(),
                        Require2FA = e.Require2FA,
                        AllowedLoginMethod = e.AllowedLoginMethod.ToString(),
                        AllowPublicRegistration = e.AllowPublicRegistration,
                        DefaultRoleName = e.DefaultRoleName ?? "End User"
                    })
                    .ToListAsync();

                return await Result<List<ClientResponse>>.SuccessAsync(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all clients for tenant {TenantId}", tenantId);
                return await Result<List<ClientResponse>>.FailAsync(ex.Message);
            }
        }
        private ClientType GetClientType(string clientype)
        {
            if (string.IsNullOrWhiteSpace(clientype))
            {
                return ClientType.Web;
            }
            var clt = Enum.TryParse<ClientType>(clientype, true, out var cltype);
            return clt ? cltype : ClientType.Web;
        }
        public async Task<DataTableResponse<ClientResponse>> GetClientPaged(DataTableRequest request)
        {
            try
            {
                var query = _dbContext.Clients.AsNoTracking();
                var response = await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new ClientResponse
                            {
                                Id = e.Id,
                                ClientId = e.ClientId,
                                ClientType = e.ClientType!,
                                IsActive = e.IsActive,
                                Audience = e.Audience,
                                Name = e.DisplayName,
                                Scopes = string.Join(",", e.ClientScopes.Select(x => x.Scope.Name).ToList()),
                                AppClientType = e.AppClientType.ToString(),
                                Require2FA = e.Require2FA,
                                AllowedLoginMethod = e.AllowedLoginMethod.ToString(),
                                AllowPublicRegistration = e.AllowPublicRegistration,
                                DefaultRoleName = e.DefaultRoleName ?? "End User",
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
                                nameof(ApplicationClient.ClientId),
                                nameof(ApplicationClient.DisplayName),
                                nameof(ApplicationClient.AppClientType),
                                nameof(ApplicationClient.CreatedBy),
                                nameof(ApplicationClient.CreatedOn),
                                nameof(ApplicationClient.LastModifiedBy),
                                nameof(ApplicationClient.LastModifiedOn),
                                nameof(ApplicationClient.IPAddress),
                                nameof(ApplicationClient.IsDeleted)
                            },
                            CancellationToken.None);

                if (response?.Data != null)
                {
                    foreach (var client in response.Data)
                    {
                        if (_memoryCache.TryGetValue($"client-health-{client.ClientId}", out ClientHealthStatus health))
                        {
                            client.IsOnline = health.IsOnline;
                            client.LastCheckTime = health.LastCheck.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new DataTableResponse<ClientResponse>
                {
                    Data = new List<ClientResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }

        }

        public Task<PaginatedResult<ClientResponse>> GetPagedAsync(ClientPagedRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<ClientRequest>> GetByIdAsync(Guid clientId)
        {
            try
            {
                var client = await _dbContext.Clients
                    .Include(c => c.TenantClients)
                    .Include(c => c.ClientScopes)
                        .ThenInclude(cs => cs.Scope)
                            .ThenInclude(s => s.Permissions)
                                .ThenInclude(p => p.Permission)
                    .FirstOrDefaultAsync(c => c.Id == clientId);

                if (client == null)
                    return await Result<ClientRequest>.FailAsync("Client not found.");

                var request = new ClientRequest
                {
                    Id = client.Id,
                    ClientId = client.ClientId,
                    ClientName = client.DisplayName,
                    Audience = client.Audience,
                    ClientType = client.AppClientType,
                    Require2FA = client.Require2FA,
                    AllowedLoginMethod = client.AllowedLoginMethod,
                    AllowPublicRegistration = client.AllowPublicRegistration,
                    DefaultRoleName = client.DefaultRoleName ?? "End User",
                    CreatedBy = client.CreatedBy,
                    CreatedOn = client.CreatedOn,
                    LastModifiedBy = client.LastModifiedBy,
                    LastModifiedOn = client.LastModifiedOn,
                    IPAddress = client.IPAddress,
                    IsDeleted = client.IsDeleted,
                    RedirectUris = (await _appManager.GetRedirectUrisAsync(client)).ToList(),
                    PostLogoutRedirectUris = (await _appManager.GetPostLogoutRedirectUrisAsync(client)).ToList(),
                    Scopes = client.ClientScopes.Select(cs => new ScopeRequest
                    {
                        Name = cs.Scope.Name,
                        DisplayName = cs.Scope.DisplayName,
                        Permissions = cs.Scope.Permissions
                            .Where(p => p.Permission.ClientApplicationId == clientId)
                            .Select(p => new Application.Requests.Features.Permissions
                            {
                                Code = p.Permission.Code,
                                Description = p.Permission.Description
                            }).ToList()
                    }).ToList()
                };

                return await Result<ClientRequest>.SuccessAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client by id {ClientId}", clientId);
                return await Result<ClientRequest>.FailAsync(ex.Message);
            }
        }

        public Task<Result<string>> RotateSecretAsync(Guid clientId)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<ClientResponse>> GetDefaultClientAsync(CancellationToken cancellationToken = default)
        {
            var res = await _dbContext.Clients.Where(x => x.ClientId == _defaultSetting.ClientId).Select(x => new ClientResponse()
            {
                AppClientType = x.AppClientType.ToString(),
                ClientId = x.ClientId,
                Audience = x.Audience,
                ClientType = x.ClientType!,
                Id = x.Id,
                IsActive = x.IsActive,
                Name = x.DisplayName,
                Scopes = string.Join(",", x.ClientScopes.Select(s => s.Scope.Name).ToList())
            }).FirstOrDefaultAsync(cancellationToken);

            return await Result<ClientResponse>.SuccessAsync(res);
        }
    }
}
