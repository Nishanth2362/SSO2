using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit.Cryptography;
using SSO.Application.Features.Tenants.Commands.AddEdit;
using SSO.Application.Interfaces.Services.Features;
using SSO.Common.Constants.Permission;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using SSO.Infrastructure.Models;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SSO.Infrastructure
{
    public class DbInitializers
    {
        private readonly IClientService _clientService;
        private readonly IMediator _mediator;
        private readonly ILogger<DbInitializers> _logger;
        private readonly IConfiguration _configuration;
        private readonly DefaultSetting _defaultSetting;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _dbContext;

        public DbInitializers(
            IClientService clientService,
            IMediator mediator,
            ILogger<DbInitializers> logger,
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext dbContext)
        {
            _clientService = clientService;
            _mediator = mediator;
            _logger = logger;
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _defaultSetting = _configuration.GetSection("DefaultSetting").Get<DefaultSetting>()!;

        }

        public async Task Initialize()
        {
            var tenantId = await SeedDefaultTenant();
            if (tenantId != Guid.Empty)
            {
                await SeedDefaultSubscription(tenantId);
                var clientId = await SeedDefaultClient(tenantId);
                if (clientId == Guid.Empty)
                {
                    var existingClient = await _dbContext.Clients.FirstOrDefaultAsync(x => x.ClientId == _defaultSetting.ClientId);
                    if (existingClient != null)
                    {
                        clientId = existingClient.Id;
                    }
                }

                if (clientId != Guid.Empty)
                {
                    await SyncDefaultClientPermissions(clientId);
                    await SeedDefaultRole(tenantId, clientId);
                    await SeedDefaultSSOAdminViewerRole(tenantId, clientId);
                    await SeedDefaultUser(tenantId);
                }
            }

            // Seed default global Token Lifetime Settings
            await SeedDefaultTokenSettings();
        }
        private async Task SyncDefaultClientPermissions(Guid clientId)
        {
            try
            {
                var scope = await _dbContext.Scopes.FirstOrDefaultAsync(x => x.Name == _defaultSetting.ScopesName);
                if (scope == null) return;

                var registeredPermissions = SSO.Common.Constants.Permission.Permissions.GetRegisteredPermissions();

                foreach (var code in registeredPermissions)
                {
                    var permission = await _dbContext.Permissions
                        .FirstOrDefaultAsync(p => p.Code == code && p.ClientApplicationId == clientId);

                    if (permission == null)
                    {
                        permission = new Permission
                        {
                            Id = Guid.NewGuid(),
                            Code = code,
                            Description = code,
                            ClientApplicationId = clientId
                        };
                        _dbContext.Permissions.Add(permission);
                        await _dbContext.SaveChangesAsync();
                    }

                    var hasScopePermission = await _dbContext.ApplicationScopePermissions
                        .AnyAsync(sp => sp.ScopeId == scope.Id && sp.PermissionId == permission.Id);

                    if (!hasScopePermission)
                    {
                        _dbContext.ApplicationScopePermissions.Add(new ApplicationScopePermission
                        {
                            Id = Guid.NewGuid(),
                            ScopeId = scope.Id,
                            PermissionId = permission.Id
                        });
                        await _dbContext.SaveChangesAsync();
                    }

                    var adminRole = await _roleManager.FindByNameAsync(_defaultSetting.DefaultRole);
                    if (adminRole != null)
                    {
                        var hasRolePermission = await _dbContext.RolePermissions
                            .AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id && rp.ApplicationClientId == clientId);

                        if (!hasRolePermission)
                        {
                            _dbContext.RolePermissions.Add(new RolePermission
                            {
                                Id = Guid.NewGuid(),
                                RoleId = adminRole.Id,
                                PermissionId = permission.Id,
                                ApplicationClientId = clientId
                            });
                            await _dbContext.SaveChangesAsync();
                        }
                    }

                    const string viewerRoleName = "SSO Admin Viewer";
                    var viewerRole = await _roleManager.FindByNameAsync(viewerRoleName);
                    var viewerPermissionCodes = SSO.Common.Constants.Permission.Permissions.GetSSOAdminViewerPermissions();
                    if (viewerRole != null && viewerPermissionCodes.Contains(code))
                    {
                        var hasRolePermission = await _dbContext.RolePermissions
                            .AnyAsync(rp => rp.RoleId == viewerRole.Id && rp.PermissionId == permission.Id && rp.ApplicationClientId == clientId);

                        if (!hasRolePermission)
                        {
                            _dbContext.RolePermissions.Add(new RolePermission
                            {
                                Id = Guid.NewGuid(),
                                RoleId = viewerRole.Id,
                                PermissionId = permission.Id,
                                ApplicationClientId = clientId
                            });
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while synchronizing default client permissions.");
            }
        }

        private async Task<Guid> SeedDefaultTenant()
        {
            try
            {
                var res = await _mediator.Send(new AddEditTenentCommand()
                {
                    Code = _defaultSetting.TenantCode,
                    Name = _defaultSetting.TenantName,
                    DatabaseMode = _defaultSetting.DatabaseMode,
                    IsActive = true,
                    ConnectionString = _defaultSetting.ConnectionString,
                    FaviconUrl = _defaultSetting.FavIconUrl,
                    LogoUrl = _defaultSetting.TenantLogo,
                    BillingAddress = _defaultSetting.BillingAddress,
                    Email = _defaultSetting.Email,
                    Phone = _defaultSetting.Phone,
                    Website = _defaultSetting.Website,
                    GracePeriodDays = 7,
                    Currency = "INR"
                });
                
                if (res.Succeeded)
                {
                    return res.Data;
                }
                else
                {
                    // If tenant already exists, find it by code
                    var existingTenant = await _dbContext.Tenants
                        .FirstOrDefaultAsync(x => x.Code == _defaultSetting.TenantCode);
                    return existingTenant?.Id ?? Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the default tenant.");
                return Guid.Empty;
            }
        }

        private async Task<Guid> SeedDefaultClient(Guid tenantId)
        {
            try
            {
                var res = await _clientService.CreateAsync(new Application.Requests.Features.ClientRequest()
                {
                    ClientId = _defaultSetting.ClientId,
                    ClientName = _defaultSetting.ClientName,
                    ClientSecret = _defaultSetting.ClientSecret,
                    ClientType = _defaultSetting.ClientType,
                    PostLogoutRedirectUris = new List<string>() { _defaultSetting.PostLogoutRedirectUris },
                    RedirectUris = new List<string>() { _defaultSetting.RedirectUris },
                    Scopes = new List<Application.Requests.Features.ScopeRequest>()
                     {
                         new Application.Requests.Features.ScopeRequest()
                         {
                             Name = _defaultSetting.ScopesName,
                             DisplayName = _defaultSetting.ScopesName,
                              Permissions = SSO.Common.Constants.Permission.Permissions.GetRegisteredPermissions().Select(x=>new Application.Requests.Features.Permissions()
                              {
                                    Code = x,
                                    Description=x
                              }).ToList()
                         }
                     }
                });
                if (!res.Succeeded)
                {
                    _logger.LogError("Failed to seed default client: {Message}", string.Join(", ", res.Messages));
                    return Guid.Empty;
                }

                var result = _dbContext.TenantClients.FirstOrDefault(x => x.ApplicationClientId == res.Data && x.TenantId == tenantId);
                if(result == null)
                {
                    _dbContext.TenantClients.Add(new TenantClient
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ApplicationClientId = res.Data
                    });
                    await _dbContext.SaveChangesAsync();
                }
                return res.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the default client.");
                return Guid.Empty;
            }

        }
        private async Task SeedDefaultRole(Guid tenantId, Guid clientId)
        {
            if (clientId == Guid.Empty) return;
            try
            {
                if (!await _roleManager.RoleExistsAsync(_defaultSetting.DefaultRole))
                {
                    var role = new ApplicationRole
                    {
                        Name = _defaultSetting.DefaultRole,
                        Description = "Administrator role with full permissions",
                        TenantId = tenantId
                    };
                    await _roleManager.CreateAsync(role);

                    var scope = await _dbContext.Scopes.FirstOrDefaultAsync(x => x.Name == _defaultSetting.ScopesName);
                    if (scope != null)
                    {
                        var applicationScopePermissions = await _dbContext.ApplicationScopePermissions.Where(x => x.ScopeId == scope.Id).ToListAsync();
                        foreach (var permission in applicationScopePermissions)
                        {
                            await _dbContext.RolePermissions.AddAsync(new RolePermission
                            {
                                Id = Guid.NewGuid(),
                                RoleId = role.Id,
                                PermissionId = permission.PermissionId,
                                ApplicationClientId = clientId,
                            });
                        }
                        await _dbContext.SaveChangesAsync(CancellationToken.None);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the default role.");
            }
        }

        /// <summary>
        /// Seeds a read-only "SSO Admin Viewer" role that grants sub-administrators
        /// the ability to log into SSO Admin and view (but not modify) data.
        /// Assign this role to users who need limited SSO Admin access.
        /// </summary>
        private async Task SeedDefaultSSOAdminViewerRole(Guid tenantId, Guid clientId)
        {
            if (clientId == Guid.Empty) return;
            const string viewerRoleName = "SSO Admin Viewer";
            try
            {
                if (await _roleManager.RoleExistsAsync(viewerRoleName))
                    return; // already seeded

                var viewerRole = new ApplicationRole
                {
                    Name = viewerRoleName,
                    Description = "Read-only access to SSO Admin portal",
                    TenantId = tenantId,
                    IsSystemRole = true
                };
                await _roleManager.CreateAsync(viewerRole);

                // Resolve the permission codes and find their Permission records in the DB


                var permissionIds =await _dbContext.Permissions.Where(x => x.ClientApplicationId == clientId).Select(x => x.Id).ToListAsync();


                foreach (var perm in permissionIds)
                {
                    await _dbContext.RolePermissions.AddAsync(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = viewerRole.Id,
                        PermissionId = perm,
                        ApplicationClientId = clientId
                    });
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
                _logger.LogInformation("Seeded '{Role}' role with {Count} SSO Admin viewer permissions.",
                    viewerRoleName, permissionIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the SSO Admin Viewer role.");
            }
        }

        private async Task SeedDefaultUser(Guid tenantId)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(_defaultSetting.DefaultEmail);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = _defaultSetting.DefaultUserName,
                        Email = _defaultSetting.DefaultEmail,
                        PhoneNumber = _defaultSetting.DefaultPhoneNumber,
                        IsActive = true,
                        EmailConfirmed = true,
                        TenantId = tenantId,
                        Name = "Default Admin"
                    };
                    var result = await _userManager.CreateAsync(user, _defaultSetting.DefaultPassword);
                    var role = await _roleManager.FindByNameAsync(_defaultSetting.DefaultRole);
                    if (result.Succeeded)
                    {
                        await _dbContext.UserRoles.AddAsync(new ApplicationUserRole()
                        {
                            UserId = user.Id,
                            TenantId = tenantId,
                            RoleId = role!.Id
                        });
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the default user.");
            }
        }

        private async Task SeedDefaultSubscription(Guid tenantId)
        {
            try
            {
                var hasSubscription = await _dbContext.Subscriptions.AnyAsync(x => x.Name == _defaultSetting.SubscriptionName);
                if (!hasSubscription)
                {
                    await _dbContext.Subscriptions.AddAsync(new Subscriptions
                    {
                        Name = _defaultSetting.SubscriptionName,
                        Description = "Default subscription with unlimited access",
                        MaxApps = int.MaxValue,
                        MaxUsers = int.MaxValue,
                        AllowSeparateDb = false,
                        Currency = "INR",
                        Price = 0
                    });

                    _dbContext.SaveChanges();
                    var subs = await _dbContext.Subscriptions.FirstOrDefaultAsync(x => x.Name == _defaultSetting.SubscriptionName);
                    var hasTanantSubscription = await _dbContext.TenantSubscriptions.AnyAsync(x => x.TenantId == tenantId);
                    if (!hasTanantSubscription)
                    {
                        _dbContext.TenantSubscriptions.Add(new TenantSubscription
                        {
                            SubscriptionId = subs.Id,
                            TenantId = tenantId,
                            IsActive = true,
                            StartDateUtc = DateTime.UtcNow.AddDays(-1),
                            EndDateUtc = DateTime.UtcNow.AddYears(1)
                        });
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the default subscription.");
            }
        }

        private async Task SeedDefaultTokenSettings()
        {
            try
            {
                var existingSetting = await _dbContext.TokenLifetimeSettings.FirstOrDefaultAsync(x => x.TenantId == null);
                if (existingSetting == null)
                {
                    var configSection = _configuration.GetSection("TokenSettings");
                    var accessTokenLifetime = configSection.GetValue<int?>("AccessTokenLifetimeMinutes") ?? 60;
                    var refreshTokenLifetime = configSection.GetValue<int?>("RefreshTokenLifetimeDays") ?? 14;
                    var authCodeLifetime = configSection.GetValue<int?>("AuthorizationCodeLifetimeMinutes") ?? 5;

                    var defaultSetting = new TokenLifetimeSetting
                    {
                        Id = Guid.NewGuid(),
                        AccessTokenLifetimeMinutes = accessTokenLifetime,
                        RefreshTokenLifetimeDays = refreshTokenLifetime,
                        AuthorizationCodeLifetimeMinutes = authCodeLifetime,
                        TenantId = null,
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = "System_AutoSeeder"
                    };

                    await _dbContext.TokenLifetimeSettings.AddAsync(defaultSetting);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Seeded default TokenLifetimeSettings successfully (Access Token: {AccessMins}m, Refresh Token: {RefreshDays}d, Auth Code: {CodeMins}m).",
                        accessTokenLifetime, refreshTokenLifetime, authCodeLifetime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding default TokenLifetimeSettings.");
            }
        }
    }
}
