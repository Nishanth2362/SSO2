using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.Storage;
using MySqlX.XDevAPI;
using SSO.Application.Features.Tenants.Queries.GetPaged;
using SSO.Shared.Wrapper.Mediator;
using SSO.WebApplication.Models;
using System.Diagnostics;
using SSO.Application.Extensions;
using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Features.Tenants.Commands.AddEdit;
using SSO.Application.Features.Roles.Queries.GetPaged;
using SSO.Application.Features.Users.Commands.AddEdit;
using SSO.Application.Features.Users.Commands.Delete;
using SSO.Application.Features.Users.Queries.GetById;
using SSO.Application.Features.Users.Queries.GetPaged;
using SSO.Application.Features.Templates.Queries.GetPaged;
using SSO.Application.Features.EmailTemplates.Commands.AddEdit;
using SSO.Application.Features.EmailTemplates.Commands.Delete;
using SSO.Application.Features.EmailTemplates.Queries.GetById;
using SSO.Application.Features.EmailTemplates.Queries.GetPaged;
using SSO.Application.Features.Subscriptions.Queries.GetPaged;
using SSO.Application.Features.Subscriptions.Commands.AddEdit;
using SSO.Application.Features.Subscriptions.Commands.Delete;
using SSO.Application.Features.Subscriptions.Queries.GetById;
using SSO.Application.Features.Tenants.Commands.Delete;
using SSO.Application.Features.Tenants.Queries.GetById;
using SSO.Application.Features.Tenants.Queries.GetAll;
using SSO.Application.Requests.Features;
using SSO.Application.Responses.Features;
using Permissions = SSO.Common.Constants.Permission.Permissions;
using SSO.Application.Interfaces.Repos;
using SSO.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using SSO.Application.Features.TenentSubscriptions.Commands.AddEdit;
using SSO.Application.Features.Templates.Queries.GetById;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests;
using SSO.Domain.Entities;
using SSO.Common.Wrapper;
using Microsoft.AspNetCore.Diagnostics;
using SSO.Application.Requests.DataTable;
using Microsoft.AspNetCore.Identity;
namespace SSO.WebApplication.Controllers;

[Authorize]
public class HomeController : Controller
{
    private const long ImportFileMaxBytes = 10 * 1024 * 1024;
    private const long TemplateFileMaxBytes = 15 * 1024 * 1024;
    private readonly IMediator _mediator;
    private readonly IClientService _clientService;
    private readonly IUnitOfWork<Guid> _unitOfWork;
    private readonly ApplicationDbContext _dbContext;
    private readonly IUploadService _uploadService;
    private readonly IExcelService _excelService;
    private readonly IScopeServices _scopeServices;
    private readonly OpenIddict.Abstractions.IOpenIddictTokenManager _tokenManager;
    private readonly IDataTableService _dataTableService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITokenLifetimeSettingsService _tokenSettingsService;

    public HomeController(
        IMediator mediator,
        IClientService clientService,
        IUnitOfWork<Guid> unitOfWork,
        ApplicationDbContext dbContext,
        OpenIddict.Abstractions.IOpenIddictTokenManager tokenManager,
        IUploadService uploadService,
        IExcelService excelService,
        IScopeServices scopeServices,
        IDataTableService dataTableService,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService,
        ITokenLifetimeSettingsService tokenSettingsService)
    {
        _mediator = mediator;
        _clientService = clientService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _tokenManager = tokenManager;
        _uploadService = uploadService;
        _excelService = excelService;
        _scopeServices = scopeServices;
        _dataTableService = dataTableService;
        _userManager = userManager;
        _currentUserService = currentUserService;
        _tokenSettingsService = tokenSettingsService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Client.Edit)]
    public IActionResult ImportScopes(Guid clientId)
    {
        ViewBag.ClientId = clientId;
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Client.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportScopes(IFormFile file, [FromQuery] Guid clientId)
    {
        try
        {
            if (file == null || file.Length == 0)
                return Json(await Result<int>.FailAsync("No file uploaded."));

            var importValidationError = ValidateFormFile(file, ImportFileMaxBytes, ".xlsx", ".xls");
            if (importValidationError != null)
                return Json(await Result<int>.FailAsync(importValidationError));

            using var stream = file.OpenReadStream();
            var result = await _scopeServices.ImportScopesAsync(stream, clientId);
            return Json(result);
        }
        catch (Exception ex)
        {
            return Json(await Result<int>.FailAsync($"Unexpected error: {ex.Message}"));
        }
    }
    public async Task<IActionResult> Index()
    {
        var result = await _mediator.Send(new SSO.Application.Features.Dashboard.Queries.GetDashboardQuery());
        return View(result);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> Tenants()
    {

        return View();
    }

[Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> CreateTenant(Guid Id)
    {
        var model = new AddEditTenentCommand();
        if (Id != Guid.Empty)
        {
            var result = await _mediator.Send(new GetByIdTenantQuery(Id));
            if (result.Succeeded)
            {
                model = new AddEditTenentCommand
                {
                    Id = result.Data.Id,
                    Code = result.Data.Code,
                    Name = result.Data.Name,
                    DatabaseMode = result.Data.DatabaseMode,
                    DatabaseProvider = result.Data.DatabaseProvider,
                    DatabaseName = result.Data.DatabaseName,
                    IsActive = result.Data.IsActive,
                    LogoUrl = result.Data.LogoUrl,
                    FaviconUrl = result.Data.FaviconUrl,
                    Email = result.Data.Email,
                    Phone = result.Data.Phone,
                    Website = result.Data.Website,
                    BillingAddress = result.Data.BillingAddress,
                    BackgroundText = result.Data.BackgroundText,
                    ClientIds = result.Data.ClientIds
                };
            }
        }

        var clients = await _dbContext.Clients.AsNoTracking().ToListAsync();
        ViewBag.Clients = clients;

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Tenant.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant(AddEditTenentCommand command, IFormFile? LogoFile, IFormFile? FaviconFile)
    {
        if (LogoFile != null && LogoFile.Length > 0)
        {
            var logoValidationError = ValidateFormFile(LogoFile, 2 * 1024 * 1024, ".png", ".jpg", ".jpeg", ".webp");
            if (logoValidationError != null)
                return Json(await Result<Guid>.FailAsync(logoValidationError));

            using var ms = new MemoryStream();
            await LogoFile.CopyToAsync(ms);
            command.LogoUrl = _uploadService.UploadAsync(new UploadRequest
            {
                Data = ms.ToArray(),
                FileName = LogoFile.FileName,
                Extension = Path.GetExtension(LogoFile.FileName),
                UploadType = SSO.Application.Enums.UploadType.TenantLogo
            });
        }

        if (FaviconFile != null && FaviconFile.Length > 0)
        {
            var faviconValidationError = ValidateFormFile(FaviconFile, 2 * 1024 * 1024, ".png", ".jpg", ".jpeg", ".webp", ".ico");
            if (faviconValidationError != null)
                return Json(await Result<Guid>.FailAsync(faviconValidationError));

            using var ms = new MemoryStream();
            await FaviconFile.CopyToAsync(ms);
            command.FaviconUrl = _uploadService.UploadAsync(new UploadRequest
            {
                Data = ms.ToArray(),
                FileName = FaviconFile.FileName,
                Extension = Path.GetExtension(FaviconFile.FileName),
                UploadType = SSO.Application.Enums.UploadType.TenantFavicon
            });
        }

        var result = await _mediator.Send(command);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Tenant.Delete)]
    public async Task<IActionResult> DeleteTenant(Guid Id)
    {
        var model = new DeleteTenantCommand { Id = Id };
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Tenant.Delete)]
    public async Task<IActionResult> DeleteTenant(DeleteTenantCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }
    [HttpPost]
    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> GetTenants()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(
            new GetPagedTenantQuery(request));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Subscription.View)]
    public IActionResult Subscriptions()
    {
        return View();
    }

    [Authorize(Policy = Permissions.Client.View)]
    public async Task<IActionResult> Clients()
    {
        return View();
    }

    [Authorize(Policy = Permissions.Client.View)]
    public async Task<IActionResult> CreateClient(Guid Id)
    {
        var model = new ClientRequest();
        if (Id != Guid.Empty)
        {
            var result = await _clientService.GetByIdAsync(Id);
            if (result.Succeeded)
            {
                model = result.Data;
            }
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Client.Create)]
    public async Task<IActionResult> CreateClient([FromBody] ClientRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(await Result<Guid>.FailAsync(errors));
        }

        var result = await _clientService.CreateAsync(request);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Client.Delete)]
    public async Task<IActionResult> DeleteClient(Guid Id)
    {
        return View(Id);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Client.Delete)]
    public async Task<IActionResult> DeleteClient(Guid Id, bool confirm = true)
    {
        var result = await _clientService.DeleteAsync(Id);
        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Client.View)]
    public async Task<IActionResult> GetClients()
    {
        var request = Request.ToDataTableRequest();
        var result = await _clientService.GetClientPaged(request);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> Users()
    {
        var isMasterTenant = _currentUserService.IsMasterTenant;
        var tenantId = _currentUserService.TenantId;

        var tenants = isMasterTenant 
            ? (await _mediator.Send(new GetAllTenantQuery())).Data 
            : new List<TenantResponse>();
        ViewBag.Tenants = tenants;

        var roles = isMasterTenant
            ? await _dbContext.Roles.AsNoTracking().ToListAsync()
            : await _dbContext.Roles.AsNoTracking().Where(r => r.TenantId == tenantId).ToListAsync();
        ViewBag.Roles = roles;

        var clients = isMasterTenant
            ? await _dbContext.Clients.AsNoTracking().ToListAsync()
            : await _dbContext.Clients.AsNoTracking().Where(c => c.TenantClients.Any(tc => tc.TenantId == tenantId)).ToListAsync();
        ViewBag.Clients = clients;

        ViewBag.IsMasterTenant = isMasterTenant;

        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.ResetPassword)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPasswordAdmin(Guid userId, string newPassword)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(newPassword))
        {
            return Json(await Result<string>.FailAsync("Invalid user ID or new password."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Json(await Result<string>.FailAsync("User not found."));
        }

        // Check tenant boundary for security if the logged-in user is not a master tenant
        if (!_currentUserService.IsMasterTenant && user.TenantId != _currentUserService.TenantId)
        {
            return Json(await Result<string>.FailAsync("Access Denied. You do not have permission to manage this user's password."));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
        {
            return Json(await Result<string>.SuccessAsync("Password changed successfully."));
        }

        var errors = result.Errors.Select(e => e.Description).ToList();
        return Json(await Result<string>.FailAsync(errors));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> GetUsers()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new SSO.Application.Features.Users.Queries.GetPaged.GetPagedUserQuery(request));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser(Guid Id)
    {
        var model = new AddEditUserCommand();
        var requestingUserTenantId = _currentUserService.TenantId;
        var isMasterTenant = _currentUserService.IsMasterTenant;
        ViewBag.RequestingUserTenantId = requestingUserTenantId;
        ViewBag.IsMasterTenant = isMasterTenant;

        // Retrieve returnUrl to determine auto-selection of Tenant
        Guid? autoSelectTenantId = null;
        string? returnUrl = Request.Query["returnUrl"];
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = Request.Cookies["SSO_ReturnUrl"];
        }

        string? tenantIdStr = Request.Query["tenant_id"];
        if (string.IsNullOrEmpty(tenantIdStr))
        {
            tenantIdStr = Request.Query["tenantId"];
        }

        if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var queryTenantId))
        {
            autoSelectTenantId = queryTenantId;
        }
        else
        {
            string? clientId = Request.Query["client_id"];
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = Request.Query["clientId"];
            }

            if (string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(returnUrl))
            {
                try
                {
                    var uri = returnUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                        ? new Uri(returnUrl) 
                        : new Uri(new Uri($"{Request.Scheme}://{Request.Host}"), returnUrl);

                    var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                    if (queryParams.TryGetValue("client_id", out var cid))
                    {
                        clientId = cid.ToString();
                    }
                    else if (queryParams.TryGetValue("clientId", out var cid2))
                    {
                        clientId = cid2.ToString();
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }
            }

            ApplicationClient? matchingClient = null;
            if (!string.IsNullOrEmpty(clientId))
            {
                matchingClient = await _dbContext.Clients
                    .AsNoTracking()
                    .Include(c => c.TenantClients)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);
            }

            if (matchingClient == null && !string.IsNullOrEmpty(returnUrl))
            {
                matchingClient = await _dbContext.Clients
                    .AsNoTracking()
                    .Include(c => c.TenantClients)
                    .FirstOrDefaultAsync(c => EF.Functions.Like(c.RedirectUris, $"%{returnUrl}%"));
            }

            if (matchingClient != null)
            {
                autoSelectTenantId = matchingClient.TenantClients.FirstOrDefault()?.TenantId;
            }
        }

        if (Id != Guid.Empty)
        {
            var result = await _mediator.Send(new GetByIdUserQuery(Id));
            if (result.Succeeded)
            {
                model = new AddEditUserCommand
                {
                    Id = result.Data.Id,
                    Name = result.Data.FirstName,
                    UserName = result.Data.UserName,
                    Email = result.Data.Email,
                    PhoneNumber = result.Data.PhoneNumber,
                    TenantId = result.Data.TenantId,
                    ActivateUser = result.Data.IsActive,
                    AutoConfirmEmail = result.Data.IsEmailConfirmed,
                    ClientIds = result.Data.ClientIds,
                    RoleNames = result.Data.Roles
                };

                var rolesResult = await _mediator.Send(new SSO.Application.Features.Roles.Queries.GetAll.GetAllRoleQuery { TenantId = result.Data.TenantId });
                ViewBag.Roles = rolesResult.Data ?? new List<RoleResponse>();
            }
        }
        else
        {
            if (autoSelectTenantId.HasValue)
            {
                model.TenantId = autoSelectTenantId.Value;
            }
            else if (!isMasterTenant)
            {
                model.TenantId = requestingUserTenantId;
            }

            if (model.TenantId != Guid.Empty)
            {
                var rolesResult = await _mediator.Send(new SSO.Application.Features.Roles.Queries.GetAll.GetAllRoleQuery { TenantId = model.TenantId });
                ViewBag.Roles = rolesResult.Data ?? new List<RoleResponse>();
            }
        }

        ViewBag.AutoSelectTenantId = autoSelectTenantId;

        var tenants = await _mediator.Send(new GetAllTenantQuery());
        ViewBag.Tenants = tenants.Data;

        List<ApplicationClient> clients;
        if (model.TenantId != Guid.Empty)
        {
            clients = await _dbContext.Clients
                .AsNoTracking()
                .Where(c => c.TenantClients.Any(tc => tc.TenantId == model.TenantId))
                .ToListAsync();
        }
        else
        {
            clients = await _dbContext.Clients.AsNoTracking().ToListAsync();
        }
        ViewBag.Clients = clients;

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser(AddEditUserCommand command)
    {
        if (!_currentUserService.IsMasterTenant)
        {
            command.TenantId = _currentUserService.TenantId;
        }

        if (string.IsNullOrWhiteSpace(command.Origin))
            command.Origin = string.IsNullOrWhiteSpace(Request.Headers["origin"]) ? $"{Request.Scheme}://{Request.Host}" : Request.Headers["origin"].ToString();
        var result = await _mediator.Send(command);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Users.Delete)]
    public async Task<IActionResult> DeleteUser(Guid Id)
    {
        var model = new DeleteUserCommand { Id = Id };
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Delete)]
    public async Task<IActionResult> DeleteUser(DeleteUserCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Edit)]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return Json(new { succeeded = false, message = "User not found." });
        user.IsActive = !user.IsActive;
        await _dbContext.SaveChangesAsync();
        return Json(new { succeeded = true, isActive = user.IsActive, message = $"User status updated successfully." });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Edit)]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return Json(new { succeeded = false, message = "User not found." });

        var wasLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        if (!wasLocked)
            return Json(new { succeeded = false, message = "User is not currently locked." });

        // Clear the lockout and reset failed access counter
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Json(new { succeeded = true, message = $"User '{user.UserName}' has been unlocked successfully." });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Client.Edit)]
    public async Task<IActionResult> ToggleClientStatus(Guid id)
    {
        var client = await _dbContext.Clients.FindAsync(id);
        if (client == null) return Json(new { succeeded = false, message = "Application client not found." });
        client.IsActive = !client.IsActive;
        await _dbContext.SaveChangesAsync();
        return Json(new { succeeded = true, isActive = client.IsActive, message = $"Application status updated successfully." });
    }

    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<IActionResult> Roles()
    {
        var isMasterTenant = _currentUserService.IsMasterTenant;
        var tenants = isMasterTenant 
            ? (await _mediator.Send(new GetAllTenantQuery())).Data 
            : new List<TenantResponse>();
        ViewBag.Tenants = tenants;
        ViewBag.IsMasterTenant = isMasterTenant;
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<IActionResult> GetRoles()
    {
        var request = Request.ToDataTableRequest();
        var query = new GetPagedRolesQuery(request);

        if (request.Filters.TryGetValue("TenantId", out var filterTenantIdStr) && Guid.TryParse(filterTenantIdStr, out var filterTenantId))
        {
            query.TenantId = filterTenantId;
        }
        else if (request.Filters.TryGetValue("Tenant", out var filterTenantStr) && Guid.TryParse(filterTenantStr, out var filterTenantGuid))
        {
            query.TenantId = filterTenantGuid;
        }

        // Resolve TenantId for minimal users context
        var ssoMode = Request.Query["sso_mode"].ToString();
        if (string.IsNullOrEmpty(ssoMode))
        {
            ssoMode = Request.Cookies["SSO_ViewContext"];
        }

        if (!string.IsNullOrEmpty(ssoMode) && ssoMode.Equals("minimal_users", StringComparison.OrdinalIgnoreCase))
        {
            Guid? autoSelectTenantId = null;
            string? returnUrl = Request.Query["returnUrl"];
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Cookies["SSO_ReturnUrl"];
            }

            string? tenantIdStr = Request.Query["tenant_id"];
            if (string.IsNullOrEmpty(tenantIdStr)) tenantIdStr = Request.Query["tenantId"];

            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var queryTenantId))
            {
                autoSelectTenantId = queryTenantId;
            }
            else
            {
                string? clientId = Request.Query["client_id"];
                if (string.IsNullOrEmpty(clientId)) clientId = Request.Query["clientId"];

                if (string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(returnUrl))
                {
                    try
                    {
                        var uri = returnUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                            ? new Uri(returnUrl) 
                            : new Uri(new Uri($"{Request.Scheme}://{Request.Host}"), returnUrl);

                        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                        if (queryParams.TryGetValue("client_id", out var cid))
                        {
                            clientId = cid.ToString();
                        }
                        else if (queryParams.TryGetValue("clientId", out var cid2))
                        {
                            clientId = cid2.ToString();
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(clientId))
                {
                    var matchingClient = await _dbContext.Clients
                        .AsNoTracking()
                        .Include(c => c.TenantClients)
                        .FirstOrDefaultAsync(c => c.ClientId == clientId);
                    if (matchingClient != null)
                    {
                        autoSelectTenantId = matchingClient.TenantClients.FirstOrDefault()?.TenantId;
                    }
                }
            }

            if (autoSelectTenantId.HasValue)
            {
                query.TenantId = autoSelectTenantId.Value;
            }
        }

        var result = await _mediator.Send(query);

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }
    [HttpGet]
    public async Task<IActionResult> GetRolesByTenant(Guid tenantId)
    {
        var result = await _mediator.Send(new SSO.Application.Features.Roles.Queries.GetAll.GetAllRoleQuery { TenantId = tenantId });
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetClientsByTenant(Guid tenantId)
    {
        var clients = await _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.TenantClients.Any(tc => tc.TenantId == tenantId))
            .Select(c => new { Id = c.Id, DisplayName = c.DisplayName, ClientId = c.ClientId })
            .ToListAsync();
        return Json(new { succeeded = true, data = clients });
    }

    [Authorize(Policy = Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole(Guid Id)
    {
        var model = new SSO.Application.Features.Roles.Commands.AddEdit.AddEditRolesCommand();
        var requestingUserTenantId = _currentUserService.TenantId;
        var isMasterTenant = _currentUserService.IsMasterTenant;
        ViewBag.RequestingUserTenantId = requestingUserTenantId;
        ViewBag.IsMasterTenant = isMasterTenant;

        // Resolve TenantId for minimal users context
        Guid? autoSelectTenantId = null;
        var ssoMode = Request.Query["sso_mode"].ToString();
        if (string.IsNullOrEmpty(ssoMode))
        {
            ssoMode = Request.Cookies["SSO_ViewContext"];
        }

        var isMinimalMode = !string.IsNullOrEmpty(ssoMode) && ssoMode.Equals("minimal_users", StringComparison.OrdinalIgnoreCase);
        ViewBag.IsMinimalMode = isMinimalMode;

        if (isMinimalMode)
        {
            string? returnUrl = Request.Query["returnUrl"];
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.Cookies["SSO_ReturnUrl"];
            }

            string? tenantIdStr = Request.Query["tenant_id"];
            if (string.IsNullOrEmpty(tenantIdStr)) tenantIdStr = Request.Query["tenantId"];

            if (!string.IsNullOrEmpty(tenantIdStr) && Guid.TryParse(tenantIdStr, out var queryTenantId))
            {
                autoSelectTenantId = queryTenantId;
            }
            else
            {
                string? clientId = Request.Query["client_id"];
                if (string.IsNullOrEmpty(clientId)) clientId = Request.Query["clientId"];

                if (string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(returnUrl))
                {
                    try
                    {
                        var uri = returnUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                            ? new Uri(returnUrl) 
                            : new Uri(new Uri($"{Request.Scheme}://{Request.Host}"), returnUrl);

                        var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                        if (queryParams.TryGetValue("client_id", out var cid))
                        {
                            clientId = cid.ToString();
                        }
                        else if (queryParams.TryGetValue("clientId", out var cid2))
                        {
                            clientId = cid2.ToString();
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(clientId))
                {
                    var matchingClient = await _dbContext.Clients
                        .AsNoTracking()
                        .Include(c => c.TenantClients)
                        .FirstOrDefaultAsync(c => c.ClientId == clientId);
                    if (matchingClient != null)
                    {
                        autoSelectTenantId = matchingClient.TenantClients.FirstOrDefault()?.TenantId;
                    }
                }
            }
        }

        if (Id != Guid.Empty)
        {
            var result = await _mediator.Send(new SSO.Application.Features.Roles.Queries.GetById.GetByIdRoleQuery { Id = Id });
            if (result.Succeeded)
            {
                model = new SSO.Application.Features.Roles.Commands.AddEdit.AddEditRolesCommand
                {
                    Id = result.Data.Id,
                    Name = result.Data.Name,
                    Description = result.Data.Description,
                    TenantId = result.Data.TenantId,
                    IsSystemRole = result.Data.IsSystemRole
                };
            }
        }
        else
        {
            if (autoSelectTenantId.HasValue)
            {
                model.TenantId = autoSelectTenantId.Value;
            }
            else if (!isMasterTenant)
            {
                model.TenantId = requestingUserTenantId;
            }
        }

        ViewBag.AutoSelectTenantId = autoSelectTenantId;

        var tenants = await _mediator.Send(new GetAllTenantQuery());
        ViewBag.Tenants = tenants.Data;
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole(SSO.Application.Features.Roles.Commands.AddEdit.AddEditRolesCommand command)
    {
        if (!_currentUserService.IsMasterTenant)
        {
            command.TenantId = _currentUserService.TenantId;
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(await Result<Guid>.FailAsync(errors));
        }

        var result = await _mediator.Send(command);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Roles.Delete)]
    public async Task<IActionResult> DeleteRole(Guid Id)
    {
        var model = new SSO.Application.Features.Roles.Commands.Delete.DeleteRoleCommand { Id = Id };
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Delete)]
    public async Task<IActionResult> DeleteRole(SSO.Application.Features.Roles.Commands.Delete.DeleteRoleCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Templates.View)]
    public async Task<IActionResult> Templates()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Templates.View)]
    public async Task<IActionResult> GetTemplates()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new SSO.Application.Features.Templates.Queries.GetPaged.GetPagedTemplateQuery(request));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Templates.Create)]
    public async Task<IActionResult> CreateTemplate(Guid Id)
    {
        var model = new SSO.Application.Features.Templates.Commands.AddEdit.AddEditTemplateCommand();
        if (Id != Guid.Empty)
        {
            var result = await _mediator.Send(new GetByIDTemplateQuery(Id));
            if (result.Succeeded && result.Data != null)
            {
                model.Id = result.Data.Id;
                model.Name = result.Data.Name;
                model.Key = result.Data.Key;
                model.Version = result.Data.Version;
                model.Type = result.Data.Type;
                model.Content = result.Data.Content;
            }
        }
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Templates.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(SSO.Application.Features.Templates.Commands.AddEdit.AddEditTemplateCommand command, IFormFile? TemplateFile)
    {
        if (TemplateFile != null && TemplateFile.Length > 0)
        {
            var templateValidationError = ValidateFormFile(TemplateFile, TemplateFileMaxBytes, ".docx", ".xlsx", ".pptx");
            if (templateValidationError != null)
                return Json(new { succeeded = false, messages = new[] { templateValidationError } });

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Templates");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(TemplateFile.FileName).ToLowerInvariant()}";
            var path = Path.Combine(uploadDir, fileName);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await TemplateFile.CopyToAsync(stream);
            }
            command.Content = path;
        }

        if (string.IsNullOrEmpty(command.Content))
        {
            return Json(new { succeeded = false, messages = new[] { "A template file is required." } });
        }

        var result = await _mediator.Send(command);
        return Json(result);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Templates.View)]
    public async Task<IActionResult> ExportTestTemplate(string key, string? type)
    {
        var documentService = HttpContext.RequestServices.GetRequiredService<SSO.Application.Interfaces.Services.IDocumentService>();

        var dummyData = new
        {
            Firstname = "John",
            Lastname = "Doe",
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            UserName = "Admin",
            Price = "$150.00" // These properties need to match template tokens
        };

        var bytes = await documentService.ExportAsync(key, dummyData, default);
        if (bytes == null || bytes.Length == 0)
        {
            return BadRequest("Failed to export template correctly. Check if tokens match or template file is valid.");
        }

        string ext = type == "Excel" ? "xlsx" : (type == "Word" ? "docx" : "pptx");
        var contentType = "application/octet-stream";
        return File(bytes, contentType, $"{key}_Export_{DateTime.UtcNow.Ticks}.{ext}");
    }

    [Authorize(Policy = Permissions.Templates.Delete)]
    public async Task<IActionResult> DeleteTemplate(Guid Id)
    {
        var model = new SSO.Application.Features.Templates.Commands.Delete.DeleteTemplateCommand { Id = Id };
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Templates.Delete)]
    public async Task<IActionResult> DeleteTemplate(SSO.Application.Features.Templates.Commands.Delete.DeleteTemplateCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }
    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> GetSubscriptions()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new SSO.Application.Features.Subscriptions.Queries.GetPaged.GetPagedSubscriptionQuery(request));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> CreateSubscription(Guid Id)
    {
        var model = new AddEditSubscriptionCommand();
        if (Id != Guid.Empty)
        {
            var result = await _mediator.Send(new GetByIdSubscriptionQuery(Id));
            if (result.Succeeded)
            {
                model = new AddEditSubscriptionCommand
                {
                    Id = result.Data.Id,
                    Name = result.Data.Name,
                    MaxUsers = result.Data.MaxUsers,
                    MaxApps = result.Data.MaxApps,
                    AllowSeparateDb = result.Data.AllowSeparateDb,
                    BillingCycle = result.Data.BillingCycle,
                    Price = result.Data.Price,
                    Description = result.Data.Description
                };
            }
        }
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.Create)]
    public async Task<IActionResult> CreateSubscription(AddEditSubscriptionCommand command)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(await Result<Guid>.FailAsync(errors));
        }

        var result = await _mediator.Send(command);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Subscription.Delete)]
    public async Task<IActionResult> DeleteSubscription(Guid Id)
    {
        var model = new DeleteSubscriptionCommand { Id = Id };
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.Delete)]
    public async Task<IActionResult> DeleteSubscription(DeleteSubscriptionCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<IActionResult> RolePermissions(Guid Id, Guid? tenantId = null)
    {
        ViewBag.RoleId = Id;
        var roleResult = await _mediator.Send(new SSO.Application.Features.Roles.Queries.GetById.GetByIdRoleQuery { Id = Id });
        if (roleResult.Succeeded)
        {
            ViewBag.RoleName = roleResult.Data.Name;
            if (!tenantId.HasValue && roleResult.Data.TenantId != Guid.Empty)
            {
                tenantId = roleResult.Data.TenantId;
            }
        }

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            ViewBag.TenantId = tenantId.Value;
            var tenant = await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId.Value);
            if (tenant != null)
            {
                ViewBag.TenantName = tenant.Name;
            }
        }

        List<ApplicationClient> clients;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            clients = await _dbContext.Clients
                .AsNoTracking()
                .Where(c => c.TenantClients.Any(tc => tc.TenantId == tenantId.Value))
                .ToListAsync();
        }
        else if (roleResult.Succeeded && roleResult.Data.TenantId != Guid.Empty)
        {
            clients = await _dbContext.Clients
                .AsNoTracking()
                .Where(c => c.TenantClients.Any(tc => tc.TenantId == roleResult.Data.TenantId))
                .ToListAsync();
        }
        else
        {
            clients = await _dbContext.Clients.AsNoTracking().ToListAsync();
        }

        ViewBag.Clients = clients;

        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Edit)]
    public async Task<IActionResult> UpdateRolePermissions([FromBody] SSO.Application.Features.RolePermissions.Commands.AddEdit.AddEditRolePermissionCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }

    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> ManageTenantSubscriptions(Guid TenantId)
    {
        ViewBag.TenantId = TenantId;
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> GetTenantSubscriptions(Guid TenantId)
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new SSO.Application.Features.TenentSubscriptions.Queries.GetPaged.GetPagedTenantSubscriptionQuery(request, TenantId));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Subscription.Create)]
    public async Task<IActionResult> AddEditTenantSubscription(Guid Id, Guid TenantId)
    {
        var model = new SSO.Application.Features.TenentSubscriptions.Commands.AddEdit.AddEditTenantSubscriptionCommand { TenantId = TenantId };
        if (Id != Guid.Empty)
        {
            var result = await _mediator.Send(new SSO.Application.Features.TenentSubscriptions.Queries.GetById.GetByIdTenantSubscriptionQuery(Id));
            if (result.Succeeded)
            {
                model.Id = result.Data.Id;
                model.SubscriptionId = result.Data.SubscriptionId;
                model.StartDateUtc = result.Data.StartDateUtc;
                model.EndDateUtc = result.Data.EndDateUtc;
                model.IsActive = result.Data.IsActive;
            }
        }

        var subscriptions = await _mediator.Send(new SSO.Application.Features.Subscriptions.Queries.GetAll.GetAllSubscriptionQuery());
        ViewBag.Subscriptions = subscriptions.Data;

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.Create)]
    public async Task<IActionResult> AddEditTenantSubscription(AddEditTenantSubscriptionCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }

    // ─── Tenant Users Module ────────────────────────────────────────────────

    /// <summary>Tenant Users list – shows all tenants with a Manage button.</summary>
    [Authorize(Policy = Permissions.Tenant.View)]
    public IActionResult TenantUsers()
    {
        return View();
    }

    /// <summary>Returns paginated tenant list for the Tenant Users DataTable (reuses existing query).</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> GetTenantUsersList_Tenants()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new GetPagedTenantQuery(request));
        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    /// <summary>Manage page for a specific tenant – Applications + Users tabs.</summary>
    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> ManageTenant(Guid tenantId)
    {
        var tenantResult = await _mediator.Send(new GetByIdTenantQuery(tenantId));
        if (!tenantResult.Succeeded || tenantResult.Data == null)
            return NotFound();

        ViewBag.TenantId = tenantId;
        ViewBag.TenantName = tenantResult.Data.Name;
        ViewBag.TenantCode = tenantResult.Data.Code;
        ViewBag.TenantEmail = tenantResult.Data.Email;
        ViewBag.TenantIsActive = tenantResult.Data.IsActive;

        return View();
    }

    /// <summary>DataTables AJAX – users belonging to a specific tenant.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> GetTenantUsersList(Guid tenantId)
    {
        var request = Request.ToDataTableRequest();

        var query = _dbContext.Set<ApplicationUser>()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            var term = request.SearchValue.ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.Name != null && u.Name.ToLower().Contains(term)));
        }

        if (request.Filters != null &&
            request.Filters.TryGetValue("IsActive", out var isActiveVal) &&
            !string.IsNullOrEmpty(isActiveVal))
        {
            var isActiveList = new List<bool>();
            foreach (var val in isActiveVal.Split(','))
            {
                if (bool.TryParse(val, out var b)) isActiveList.Add(b);
            }
            if (isActiveList.Any())
            {
                query = query.Where(u => isActiveList.Contains(u.IsActive));
            }
        }

        var total = await _dbContext.Set<ApplicationUser>().CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);
        var filtered = await query.CountAsync();

        var data = await query
            .OrderByDescending(u => u.CreatedOn)
            .Skip(request.Start)
            .Take(request.Length)
            .Select(u => new
            {
                id = u.Id,
                userName = u.UserName,
                firstName = u.Name,
                email = u.Email,
                isActive = u.IsActive,
                createdAt = u.CreatedOn
            })
            .ToListAsync();

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = total,
            recordsFiltered = filtered,
            data = data
        });
    }

    /// <summary>JSON – applications (clients) assigned to a tenant.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> GetTenantApplications(Guid tenantId)
    {
        var apps = await _dbContext.Set<TenantClient>()
            .AsNoTracking()
            .Where(tc => tc.TenantId == tenantId)
            .Include(tc => tc.ApplicationClient)
            .Select(tc => new
            {
                id = tc.ApplicationClientId,
                displayName = tc.ApplicationClient.DisplayName,
                clientId = tc.ApplicationClient.ClientId,
                logoUrl = tc.ApplicationClient.LogoUrl,
                isActive = tc.ApplicationClient.IsActive,
                clientType = tc.ApplicationClient.ClientType,
                appType = tc.ApplicationClient.AppClientType.ToString()
            })
            .ToListAsync();

        return Json(new { succeeded = true, data = apps });
    }

    /// <summary>Dedicated page for users belonging to a specific Application & Tenant.</summary>
    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> ManageApplicationUsers(Guid applicationId, Guid? tenantId)
    {
        var appClient = await _dbContext.Set<ApplicationClient>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == applicationId);

        if (appClient == null)
            return NotFound();

        ViewBag.ApplicationId = applicationId;
        ViewBag.ApplicationDisplayName = appClient.DisplayName ?? appClient.ClientId;
        ViewBag.ApplicationClientId = appClient.ClientId;
        ViewBag.ApplicationLogoUrl = appClient.LogoUrl;
        ViewBag.ApplicationClientType = appClient.ClientType;

        Guid actualTenantId = tenantId ?? Guid.Empty;
        if (actualTenantId == Guid.Empty)
        {
            var tc = await _dbContext.Set<TenantClient>()
                .AsNoTracking()
                .FirstOrDefaultAsync(tc => tc.ApplicationClientId == applicationId);
            if (tc != null)
            {
                actualTenantId = tc.TenantId;
            }
        }

        if (actualTenantId != Guid.Empty)
        {
            var tenantResult = await _mediator.Send(new GetByIdTenantQuery(actualTenantId));
            if (tenantResult.Succeeded && tenantResult.Data != null)
            {
                ViewBag.TenantId = actualTenantId;
                ViewBag.TenantName = tenantResult.Data.Name;
                ViewBag.TenantCode = tenantResult.Data.Code;
            }
        }

        return View();
    }

    /// <summary>DataTables AJAX – users for a specific tenant+application combo (returns full user objects).</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> GetApplicationUsers(Guid tenantId, Guid clientId)
    {
        var request = Request.ToDataTableRequest();

        if (tenantId == Guid.Empty && clientId != Guid.Empty)
        {
            var tc = await _dbContext.Set<TenantClient>()
                .AsNoTracking()
                .FirstOrDefaultAsync(tc => tc.ApplicationClientId == clientId);
            if (tc != null)
            {
                tenantId = tc.TenantId;
            }
        }

        var query = _dbContext.Set<ApplicationUser>()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            var term = request.SearchValue.ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.Name != null && u.Name.ToLower().Contains(term)));
        }

        if (request.Filters != null &&
            request.Filters.TryGetValue("IsActive", out var isActiveVal) &&
            !string.IsNullOrEmpty(isActiveVal))
        {
            var isActiveList = new List<bool>();
            foreach (var val in isActiveVal.Split(','))
            {
                if (bool.TryParse(val, out var b)) isActiveList.Add(b);
            }
            if (isActiveList.Any())
            {
                query = query.Where(u => isActiveList.Contains(u.IsActive));
            }
        }

        var total = await _dbContext.Set<ApplicationUser>().CountAsync(u => u.TenantId == tenantId && !u.IsDeleted);
        var filtered = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedOn)
            .Skip(request.Start)
            .Take(request.Length)
            .ToListAsync();

        var tenant = await _dbContext.Set<SSO.Domain.Entities.Tenants>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);
        var tenantName = tenant?.Name ?? "Tenant";

        var data = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            data.Add(new
            {
                id = u.Id,
                userName = u.UserName,
                firstName = u.Name,
                email = u.Email,
                tenantId = u.TenantId,
                tenantName = tenantName,
                roles = roles.ToList(),
                isActive = u.IsActive,
                createdAt = u.CreatedOn
            });
        }

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = total,
            recordsFiltered = filtered,
            data = data
        });
    }

    // ────────────────────────────────────────────────────────────────────────
    // Token Lifetime Management Actions
    // ────────────────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> GetTokenLifetimeSettings()
    {
        var settings = await _tokenSettingsService.GetActiveSettingsAsync();
        return Json(new { succeeded = true, data = settings });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTokenLifetimeSettings([FromBody] SSO.Application.Configuaration.TokenLifetimeSettings request)
    {
        var result = await _tokenSettingsService.UpdateSettingsAsync(request);
        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetTokenLifetimeSettings()
    {
        var result = await _tokenSettingsService.ResetToDefaultsAsync();
        return Json(result);
    }

    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> Tokens()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> GetTokens()
    {
        var request = Request.ToDataTableRequest();
        var query = _dbContext.Set<SSO.Domain.Entities.ApplicationToken>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchValue))
        {
            if (!string.IsNullOrEmpty(request.SearchColumn) && !request.SearchColumn.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (request.SearchColumn.Equals("Subject", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(t => t.Subject != null && t.Subject.Contains(request.SearchValue));
                }
                else if (request.SearchColumn.Equals("ReferenceId", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(t => t.ReferenceId != null && t.ReferenceId.Contains(request.SearchValue));
                }
            }
            else
            {
                query = query.Where(t => (t.Subject != null && t.Subject.Contains(request.SearchValue)) || (t.ReferenceId != null && t.ReferenceId.Contains(request.SearchValue)));
            }
        }

        if (request.Filters != null && request.Filters.TryGetValue("Status", out var statusVal) && !string.IsNullOrEmpty(statusVal))
        {
            var statuses = statusVal.Split(',', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(t => statuses.Contains(t.Status));
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.CreationDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.CreationDate <= endOfDay);
        }

        var totalRecords = await _dbContext.Set<SSO.Domain.Entities.ApplicationToken>().CountAsync();
        var filteredRecords = await query.CountAsync();

        var data = await query.OrderByDescending(t => t.CreationDate)
            .Skip(request.Start)
            .Take(request.Length)
            .Select(t => new {
                id = t.Id,
                subject = t.Subject,
                type = t.Type,
                status = t.Status,
                creationDate = t.CreationDate,
                expirationDate = t.ExpirationDate,
                referenceId = t.ReferenceId
            })
            .ToListAsync();

        return Json(new
        {
            draw = request.Draw,
            recordsTotal = totalRecords,
            recordsFiltered = filteredRecords,
            data = data
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Delete)]
    public async Task<IActionResult> RevokeToken(Guid id)
    {
        var token = await _tokenManager.FindByIdAsync(id.ToString());
        if (token == null) return NotFound();

        await _tokenManager.TryRevokeAsync(token);
        return Json(new { succeeded = true, message = "Token revoked successfully." });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<HomeController>>();

        if (exceptionFeature?.Error != null)
        {
            logger.LogError(exceptionFeature.Error, "User-facing error page rendered for {Path}. RequestId: {RequestId}", exceptionFeature.Path, HttpContext.TraceIdentifier);
        }

        var model = BuildErrorViewModel(statusCode ?? Response.StatusCode);
        model.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        return View(model);
    }

    [Authorize(Policy = Permissions.Tenant.Export)]
    public async Task<IActionResult> ExportTenantUsers(DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _unitOfWork.Repository<SSO.Domain.Entities.Tenants>().Entities
            .Include(x => x.TenantSubscriptions).ThenInclude(ts => ts.Subscriptions)
            .AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new TenantResponse
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                IsActive = e.IsActive,
                DatabaseMode = e.DatabaseMode,
                Email = e.Email,
                Phone = e.Phone,
                Website = e.Website,
                BillingAddress = e.BillingAddress,
                LogoUrl = e.LogoUrl
            },
            "Id");

        if (data != null && data.Any())
        {
            var tenantIds = data.Select(t => t.Id).ToList();

            var appCounts = await _dbContext.Set<TenantClient>()
                .Where(tc => tenantIds.Contains(tc.TenantId))
                .GroupBy(tc => tc.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

            var userCounts = await _dbContext.Users
                .Where(u => tenantIds.Contains(u.TenantId) && !u.IsDeleted)
                .GroupBy(u => u.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

            var roleCounts = await _dbContext.Roles
                .Where(r => tenantIds.Contains(r.TenantId) && !r.IsDeleted)
                .GroupBy(r => r.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TenantId, x => x.Count);

            foreach (var tenant in data)
            {
                tenant.ApplicationsCount = appCounts.TryGetValue(tenant.Id, out var ac) ? ac : 0;
                tenant.UsersCount = userCounts.TryGetValue(tenant.Id, out var uc) ? uc : 0;
                tenant.RolesCount = roleCounts.TryGetValue(tenant.Id, out var rc) ? rc : 0;
            }
        }

        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<TenantResponse, object>>
        {
            { "Code", x => x.Code },
            { "Name", x => x.Name },
            { "Email", x => x.Email ?? "" },
            { "Phone", x => x.Phone ?? "" },
            { "Website", x => x.Website ?? "" },
            { "Billing Address", x => x.BillingAddress ?? "" },
            { "Database Mode", x => x.DatabaseMode.ToString() },
            { "Applications", x => x.ApplicationsCount },
            { "Users", x => x.UsersCount },
            { "Roles", x => x.RolesCount },
            { "Is Active", x => x.IsActive }
        }, "Tenants");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Tenants_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> ExportTenants([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _unitOfWork.Repository<SSO.Domain.Entities.Tenants>().Entities
            .Include(x => x.TenantSubscriptions).ThenInclude(ts => ts.Subscriptions)
            .AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new TenantResponse
            {
                Id = e.Id,
                Code = e.Code,
                Name = e.Name,
                IsActive = e.IsActive,
                DatabaseMode = e.DatabaseMode,
                Email = e.Email,
                Phone = e.Phone,
                Website = e.Website,
                BillingAddress = e.BillingAddress,
                LogoUrl = e.LogoUrl
            },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<TenantResponse, object>>
        {
            { "Code", x => x.Code },
            { "Name", x => x.Name },
            { "Email", x => x.Email ?? "" },
            { "Phone", x => x.Phone ?? "" },
            { "Website", x => x.Website ?? "" },
            { "Billing Address", x => x.BillingAddress ?? "" },
            { "Database Mode", x => x.DatabaseMode.ToString() },
            { "Is Active", x => x.IsActive }
        }, "Tenants");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Tenants_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    private ErrorViewModel BuildErrorViewModel(int? statusCode)
    {
        var model = new ErrorViewModel
        {
            StatusCode = statusCode
        };

        switch (statusCode)
        {
            case StatusCodes.Status403Forbidden:
                model.Title = "Access Restricted";
                model.Message = "You do not have permission to view this page or perform that action.";
                model.ActionText = "Back to Sign In";
                model.ActionUrl = "/Login";
                break;
            case StatusCodes.Status404NotFound:
                model.Title = "Page Not Found";
                model.Message = "The page or resource you requested could not be found. It may have moved or no longer be available.";
                model.ActionText = "Go to Dashboard";
                model.ActionUrl = "/Home";
                break;
            default:
                model.Title = "We Hit a Problem";
                model.Message = "Something unexpected happened while processing your request. Please try again in a moment.";
                model.ActionText = "Back to Sign In";
                model.ActionUrl = "/Login";
                break;
        }

        return model;
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> ExportUsers([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _userManager.Users.AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new UserResponse
            {
                Id = e.Id,
                UserName = e.UserName,
                Email = e.Email,
                FirstName = e.Name,
                LastName = string.Empty,
                PhoneNumber = e.PhoneNumber,
                IsActive = e.IsActive
            },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<UserResponse, object>>
        {
            { "UserName", x => x.UserName },
            { "First Name", x => x.FirstName },
            { "Last Name", x => x.LastName },
            { "Email", x => x.Email },
            { "Phone", x => x.PhoneNumber ?? "" },
            { "Is Active", x => x.IsActive }
        }, "Users");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Users_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<IActionResult> ExportRoles([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _dbContext.Roles.AsNoTracking();
        if (!_currentUserService.IsMasterTenant)
        {
            var tenantId = _currentUserService.TenantId;
            query = query.Where(x => x.TenantId == tenantId);
        }
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new RoleResponse { Id = e.Id, Name = e.Name, Description = e.Description, IsSystemRole = e.IsSystemRole },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<RoleResponse, object>>
        {
            { "Name", x => x.Name },
            { "Description", x => x.Description ?? "" },
            { "Is System Role", x => x.IsSystemRole }
        }, "Roles");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Roles_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> ExportSubscriptions([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _unitOfWork.Repository<SSO.Domain.Entities.Subscriptions>().Entities.AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new SubscriptionResponse { Id = e.Id, Name = e.Name, Price = e.Price, BillingCycle = e.BillingCycle, MaxUsers = e.MaxUsers, MaxApps = e.MaxApps, AllowSeparateDb = e.AllowSeparateDb },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<SubscriptionResponse, object>>
        {
            { "Name", x => x.Name },
            { "Price", x => x.Price },
            { "Billing Cycle", x => x.BillingCycle.ToString() },
            { "Max Users", x => x.MaxUsers },
            { "Max Apps", x => x.MaxApps },
            { "Allow Separate DB", x => x.AllowSeparateDb }
        }, "Subscriptions");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Subscriptions_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Client.View)]
    public async Task<IActionResult> ExportClients([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _dbContext.Clients.AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new { e.Id, ClientId = e.ClientId ?? "", DisplayName = e.DisplayName ?? "", ClientType = e.ClientType ?? "", AppClientType = e.AppClientType.ToString(), e.IsActive },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<dynamic, object>>
        {
            { "Client ID", x => x.ClientId },
            { "Display Name", x => x.DisplayName },
            { "OIDC Type", x => x.ClientType },
            { "App Type", x => x.AppClientType },
            { "Is Active", x => x.IsActive }
        }, "Clients");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Clients_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Templates.View)]
    public async Task<IActionResult> ExportTemplates([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _unitOfWork.Repository<SSO.Domain.Entities.Template>().Entities.AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new TemplateResponse { Id = e.Id, Name = e.Name, Key = e.Key, Type = e.Type, Version = e.Version },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<TemplateResponse, object>>
        {
            { "Name", x => x.Name },
            { "Key", x => x.Key },
            { "Type", x => x.Type.ToString() },
            { "Version", x => x.Version }
        }, "Templates");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Templates_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> ExportTokens([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _dbContext.Set<SSO.Domain.Entities.ApplicationToken>().AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new
            {
                Id = e.Id,
                Subject = e.Subject ?? "",
                Type = e.Type ?? "",
                Status = e.Status ?? "",
                CreationDate = e.CreationDate,
                ExpirationDate = e.ExpirationDate,
                ReferenceId = e.ReferenceId ?? ""
            },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<dynamic, object>>
        {
            { "Subject", x => x.Subject },
            { "Reference ID", x => x.ReferenceId },
            { "Type", x => x.Type },
            { "Status", x => x.Status },
            { "Creation Date", x => x.CreationDate.ToString("yyyy-MM-dd HH:mm:ss") },
            { "Expiration Date", x => x.ExpirationDate.HasValue ? x.ExpirationDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "" }
        }, "Tokens");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Tokens_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> ExportTenantUsers_Tenants([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _unitOfWork.Repository<SSO.Domain.Entities.Tenants>().Entities
            .Include(x => x.TenantSubscriptions).ThenInclude(ts => ts.Subscriptions)
            .AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new
            {
                Id = e.Id,
                Name = e.Name,
                Code = e.Code,
                DatabaseMode = e.DatabaseMode,
                SubscriptionName = e.TenantSubscriptions.Any(x => x.IsActive) ? e.TenantSubscriptions.First(x => x.IsActive).Subscriptions.Name : "No Active Plan",
                IsActive = e.IsActive
            },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<dynamic, object>>
        {
            { "Tenant Name", x => x.Name },
            { "Code", x => x.Code },
            { "Database Mode", x => x.DatabaseMode.ToString() },
            { "Subscription", x => x.SubscriptionName },
            { "Is Active", x => x.IsActive }
        }, "TenantUsers_Tenants");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TenantUsers_Tenants_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> ExportTenantUsers([FromBody] DataTableRequest request, [FromQuery] Guid tenantId)
    {
        request ??= new DataTableRequest();
        var query = _dbContext.Set<ApplicationUser>()
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && !u.IsDeleted);
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new
            {
                Id = e.Id,
                UserName = e.UserName,
                Email = e.Email,
                FirstName = e.Name,
                Joined = e.CreatedOn,
                IsActive = e.IsActive
            },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<dynamic, object>>
        {
            { "UserName", x => x.UserName },
            { "Name", x => x.FirstName },
            { "Email", x => x.Email },
            { "Joined", x => x.Joined.ToString("yyyy-MM-dd HH:mm:ss") },
            { "Is Active", x => x.IsActive }
        }, "TenantUsers");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TenantUsers_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }
    [Authorize(Policy = Permissions.Scope.Import)]
    public async Task<IActionResult> DownloadScopeImportTemplate()
    {
        // Define a local class or use a dictionary to ensure ExportAsync receives valid data
        var templateData = new[]
        {
            new { ScopeName = "api://my-service", DisplayName = "My Service API", PermissionCode = "service.read", PermissionDescription = "Allows reading service data" },
            new { ScopeName = "api://my-service", DisplayName = "My Service API", PermissionCode = "service.write", PermissionDescription = "Allows modifying service data" }
        };

        var mappers = new Dictionary<string, Func<dynamic, object>>
        {
            { "Scope Name", x => x.ScopeName },
            { "Display Name", x => x.DisplayName },
            { "Permission Code", x => x.PermissionCode },
            { "Permission Description", x => x.PermissionDescription }
        };

        var base64 = await _excelService.ExportAsync(templateData, mappers, "Sheet1");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Scope_Import_Template.xlsx");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadImportTemplate(string module)
    {
        string modName = string.IsNullOrWhiteSpace(module) ? "Data" : module.Trim();
        string fileName = $"{modName.Replace(" ", "_").Replace("&", "And")}_Import_Template.xlsx";

        object[] sampleData;
        Dictionary<string, Func<dynamic, object>> mappers;

        switch (modName.ToLower())
        {
            case "users":
                sampleData = new object[]
                {
                    new { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Username = "johndoe", PhoneNumber = "+1234567890", Role = "Administrator" }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "First Name", x => x.FirstName },
                    { "Last Name", x => x.LastName },
                    { "Email", x => x.Email },
                    { "Username", x => x.Username },
                    { "Phone Number", x => x.PhoneNumber },
                    { "Role", x => x.Role }
                };
                break;

            case "tenants":
            case "tenants & licenses":
            case "tenants and licenses":
                sampleData = new object[]
                {
                    new { TenantName = "Acme Corp", TenantCode = "ACME01", AdminEmail = "admin@acme.com", SubscriptionPlan = "Enterprise", MaxUsers = 100, ExpirationDate = "2027-12-31" }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "Tenant Name", x => x.TenantName },
                    { "Tenant Code", x => x.TenantCode },
                    { "Admin Email", x => x.AdminEmail },
                    { "Subscription Plan", x => x.SubscriptionPlan },
                    { "Max Users", x => x.MaxUsers },
                    { "Expiration Date", x => x.ExpirationDate }
                };
                break;

            case "subscriptions":
                sampleData = new object[]
                {
                    new { TenantName = "Acme Corp", PlanName = "Enterprise Plan", BillingCycle = "Annual", Amount = "$999.00", Status = "Active", StartDate = "2026-01-01", EndDate = "2027-01-01" }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "Tenant Name", x => x.TenantName },
                    { "Plan Name", x => x.PlanName },
                    { "Billing Cycle", x => x.BillingCycle },
                    { "Amount", x => x.Amount },
                    { "Status", x => x.Status },
                    { "Start Date", x => x.StartDate },
                    { "End Date", x => x.EndDate }
                };
                break;

            case "invoices":
            case "billing & invoices":
            case "billing and invoices":
            case "payments":
                sampleData = new object[]
                {
                    new { InvoiceNumber = "INV-2026-001", TenantName = "Acme Corp", Amount = "$500.00", Status = "Paid", DueDate = "2026-09-01", PaymentMethod = "Credit Card" }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "Invoice Number", x => x.InvoiceNumber },
                    { "Tenant Name", x => x.TenantName },
                    { "Amount", x => x.Amount },
                    { "Status", x => x.Status },
                    { "Due Date", x => x.DueDate },
                    { "Payment Method", x => x.PaymentMethod }
                };
                break;

            case "roles":
            case "roles & permissions":
            case "roles and permissions":
                sampleData = new object[]
                {
                    new { RoleName = "TenantManager", Description = "Manages tenant resources", PermissionCode = "tenants.manage", PermissionCategory = "Tenants" }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "Role Name", x => x.RoleName },
                    { "Description", x => x.Description },
                    { "Permission Code", x => x.PermissionCode },
                    { "Permission Category", x => x.PermissionCategory }
                };
                break;

            case "applications":
                sampleData = new object[]
                {
                    new { ClientId = "app-service-01", ClientName = "Main Web Portal", RedirectUris = "https://portal.example.com/callback", AllowedGrantTypes = "authorization_code", AllowedScopes = "openid profile email" }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "Client ID", x => x.ClientId },
                    { "Client Name", x => x.ClientName },
                    { "Redirect URIs", x => x.RedirectUris },
                    { "Allowed Grant Types", x => x.AllowedGrantTypes },
                    { "Allowed Scopes", x => x.AllowedScopes }
                };
                break;

            case "tokens":
                sampleData = new object[]
                {
                    new { Subject = "user@acme.com", ClientId = "app-service-01", Type = "AuthorizationCode", Status = "Valid", CreationDate = "2026-08-11", ExpirationDate = "2026-08-12" }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "Subject", x => x.Subject },
                    { "Client ID", x => x.ClientId },
                    { "Type", x => x.Type },
                    { "Status", x => x.Status },
                    { "Creation Date", x => x.CreationDate },
                    { "Expiration Date", x => x.ExpirationDate }
                };
                break;

            default:
                sampleData = new object[]
                {
                    new { ID = "1", Name = "Sample Item", Description = "Sample description text", Status = "Active", CreatedDate = DateTime.Now.ToString("yyyy-MM-dd") }
                };
                mappers = new Dictionary<string, Func<dynamic, object>>
                {
                    { "ID", x => x.ID },
                    { "Name", x => x.Name },
                    { "Description", x => x.Description },
                    { "Status", x => x.Status },
                    { "Created Date", x => x.CreatedDate }
                };
                break;
        }

        var base64 = await _excelService.ExportAsync(sampleData, mappers, "Template");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmailTemplates.View)]
    public IActionResult EmailTemplates()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmailTemplates.View)]
    public async Task<IActionResult> GetEmailTemplates()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new GetPagedEmailTemplatesQuery(request));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmailTemplates.View)]
    public async Task<IActionResult> GetEmailTemplateDetails(Guid id)
    {
        var result = await _mediator.Send(new GetByIdEmailTemplateQuery(id));
        if (result.Succeeded && result.Data != null)
        {
            return Json(result.Data);
        }
        return BadRequest(result);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.EmailTemplates.View)]
    public async Task<IActionResult> CreateEmailTemplate(Guid Id)
    {
        var model = new AddEditEmailTemplateCommand();
        if (Id != Guid.Empty)
        {
            var result = await _mediator.Send(new GetByIdEmailTemplateQuery(Id));
            if (result.Succeeded && result.Data != null)
            {
                model.Id = result.Data.Id;
                model.Name = result.Data.Name;
                model.Subject = result.Data.Subject;
                model.TemplateType = result.Data.TemplateType;
                model.TriggerEvent = result.Data.TriggerEvent;
                model.Body = result.Data.Body;
                model.IsActive = result.Data.IsActive;
            }
        }
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmailTemplates.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmailTemplate(AddEditEmailTemplateCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EmailTemplates.Delete)]
    public async Task<IActionResult> DeleteEmailTemplate(Guid id)
    {
        var result = await _mediator.Send(new DeleteEmailTemplateCommand { Id = id });
        return Json(result);
    }

    private static string? ValidateFormFile(IFormFile file, long maxBytes, params string[] allowedExtensions)
    {
        if (file.Length <= 0)
        {
            return "The uploaded file was empty.";
        }

        if (file.Length > maxBytes)
        {
            return $"The uploaded file exceeds the allowed size of {maxBytes / (1024 * 1024)} MB.";
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) ||
            !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return $"Unsupported file type. Allowed types: {string.Join(", ", allowedExtensions)}.";
        }

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GlobalSearchSuggestions(string category, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(new List<object>());
        }

        term = term.Trim().ToLower();
        var suggestions = new List<object>();

        try
        {
            if (string.Equals(category, "Users", StringComparison.OrdinalIgnoreCase))
            {
                var users = await _dbContext.Set<ApplicationUser>()
                    .AsNoTracking()
                    .Where(u => (u.UserName != null && u.UserName.ToLower().Contains(term)) || (u.Email != null && u.Email.ToLower().Contains(term)) || (u.Name != null && u.Name.ToLower().Contains(term)))
                    .Take(8)
                    .Select(u => new {
                        title = u.UserName,
                        subtitle = u.Email ?? u.Name,
                        url = $"/Home/Users?search={Uri.EscapeDataString(u.UserName ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(users);
            }
            else if (string.Equals(category, "Tenants", StringComparison.OrdinalIgnoreCase))
            {
                var tenants = await _dbContext.Tenants
                    .AsNoTracking()
                    .Where(t => t.Name.ToLower().Contains(term) || t.Code.ToLower().Contains(term) || (t.Email != null && t.Email.ToLower().Contains(term)))
                    .Take(8)
                    .Select(t => new {
                        title = t.Name,
                        subtitle = $"{t.Code} | {t.Email}",
                        url = $"/Home/Tenants?search={Uri.EscapeDataString(t.Name ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(tenants);
            }
            else if (string.Equals(category, "Roles", StringComparison.OrdinalIgnoreCase))
            {
                var roles = await _dbContext.Set<ApplicationRole>()
                    .AsNoTracking()
                    .Where(r => (r.Name != null && r.Name.ToLower().Contains(term)) || (r.Description != null && r.Description.ToLower().Contains(term)))
                    .Take(8)
                    .Select(r => new {
                        title = r.Name,
                        subtitle = r.Description ?? "System Role",
                        url = $"/Home/Roles?search={Uri.EscapeDataString(r.Name ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(roles);
            }
            else if (string.Equals(category, "Permissions", StringComparison.OrdinalIgnoreCase))
            {
                var permissions = await _dbContext.Permissions
                    .AsNoTracking()
                    .Where(p => p.Code.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)))
                    .Take(8)
                    .Select(p => new {
                        title = p.Code,
                        subtitle = p.Description ?? "Permission",
                        url = $"/Home/Roles?search={Uri.EscapeDataString(p.Code ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(permissions);
            }
            else if (string.Equals(category, "Clients", StringComparison.OrdinalIgnoreCase))
            {
                var clients = await _dbContext.Clients
                    .AsNoTracking()
                    .Where(c => (c.DisplayName != null && c.DisplayName.ToLower().Contains(term)) || (c.ClientId != null && c.ClientId.ToLower().Contains(term)))
                    .Take(8)
                    .Select(c => new {
                        title = c.DisplayName ?? c.ClientId,
                        subtitle = c.ClientId,
                        url = $"/Home/Clients?search={Uri.EscapeDataString(c.DisplayName ?? c.ClientId ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(clients);
            }
            else if (string.Equals(category, "Subscriptions", StringComparison.OrdinalIgnoreCase))
            {
                var subs = await _dbContext.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.Name.ToLower().Contains(term) || (s.Description != null && s.Description.ToLower().Contains(term)))
                    .Take(8)
                    .Select(s => new {
                        title = s.Name,
                        subtitle = $"Price: {s.Price} INR | {s.Description}",
                        url = $"/Home/Subscriptions?search={Uri.EscapeDataString(s.Name ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(subs);
            }
            else if (string.Equals(category, "AuditLogs", StringComparison.OrdinalIgnoreCase))
            {
                var logs = await _dbContext.AuditTrails
                    .AsNoTracking()
                    .Where(a => (a.TableName != null && a.TableName.ToLower().Contains(term)) || (a.Type != null && a.Type.ToLower().Contains(term)) || (a.UserId != null && a.UserId.ToLower().Contains(term)))
                    .OrderByDescending(a => a.DateTime)
                    .Take(8)
                    .Select(a => new {
                        title = $"{a.TableName} ({a.Type})",
                        subtitle = $"User: {a.UserId ?? "System"} | {a.DateTime.ToString("g")}",
                        url = $"/Reporting/AuditLogs?search={Uri.EscapeDataString(a.TableName ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(logs);
            }
            else if (string.Equals(category, "Templates", StringComparison.OrdinalIgnoreCase))
            {
                var templates = await _dbContext.Set<Template>()
                    .AsNoTracking()
                    .Where(t => t.Name.ToLower().Contains(term) || t.Key.ToLower().Contains(term))
                    .Take(8)
                    .Select(t => new {
                        title = t.Name,
                        subtitle = t.Key,
                        url = $"/Home/Templates?search={Uri.EscapeDataString(t.Name ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(templates);
            }
            else if (string.Equals(category, "EmailTemplates", StringComparison.OrdinalIgnoreCase))
            {
                var emailTemplates = await _dbContext.EmailTemplates
                    .AsNoTracking()
                    .Where(t => t.Name.ToLower().Contains(term) || t.Subject.ToLower().Contains(term))
                    .Take(8)
                    .Select(t => new {
                        title = t.Name,
                        subtitle = t.Subject,
                        url = $"/Home/EmailTemplates?search={Uri.EscapeDataString(t.Name ?? string.Empty)}"
                    })
                    .ToListAsync();
                suggestions.AddRange(emailTemplates);
            }
            else if (string.Equals(category, "Jobs", StringComparison.OrdinalIgnoreCase))
            {
                var jobsList = new List<object>();
                try
                {
                    using (var connection = JobStorage.Current.GetConnection())
                    {
                        var storageJobs = connection.GetRecurringJobs();
                        foreach (var sj in storageJobs)
                        {
                            if (sj.Id.Contains(term, StringComparison.OrdinalIgnoreCase))
                            {
                                jobsList.Add(new
                                {
                                    title = sj.Id.Replace("-", " ").Replace("_", " "),
                                    subtitle = $"Cron: {sj.Cron}",
                                    url = $"/Jobs"
                                });
                            }
                        }
                    }
                }
                catch { }
                suggestions.AddRange(jobsList.Take(8));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Search suggestions exception: {ex.Message}");
        }

        return Json(suggestions);
    }
}