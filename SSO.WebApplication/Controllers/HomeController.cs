using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public HomeController(IMediator mediator, IClientService clientService, IUnitOfWork<Guid> unitOfWork, ApplicationDbContext dbContext, OpenIddict.Abstractions.IOpenIddictTokenManager tokenManager, IUploadService uploadService, IExcelService excelService, IScopeServices scopeServices)
    {
        _mediator = mediator;
        _clientService = clientService;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _tokenManager = tokenManager;
        _uploadService = uploadService;
        _excelService = excelService;
        _scopeServices = scopeServices;
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
                    AllowPublicRegistration = result.Data.AllowPublicRegistration,
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
    public async Task<IActionResult> Subscriptions()
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
        return View();
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

    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> CreateUser(Guid Id)
    {
        var model = new AddEditUserCommand();
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
        
        var tenants = await _mediator.Send(new GetAllTenantQuery());
        ViewBag.Tenants = tenants.Data;
        
        var clients = await _dbContext.Clients.AsNoTracking().ToListAsync();
        ViewBag.Clients = clients;
        
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser(AddEditUserCommand command)
    {
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

    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<IActionResult> Roles()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<IActionResult> GetRoles()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new GetPagedRolesQuery(request));

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

    [Authorize(Policy = Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole(Guid Id)
    {
        var model = new SSO.Application.Features.Roles.Commands.AddEdit.AddEditRolesCommand();
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
        
        var tenants = await _mediator.Send(new GetAllTenantQuery());
        ViewBag.Tenants = tenants.Data;
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Create)]
    public async Task<IActionResult> CreateRole(SSO.Application.Features.Roles.Commands.AddEdit.AddEditRolesCommand command)
    {
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
        
        var dummyData = new { 
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
    public async Task<IActionResult> RolePermissions(Guid Id)
    {
        ViewBag.RoleId = Id;
        var roleResult = await _mediator.Send(new SSO.Application.Features.Roles.Queries.GetById.GetByIdRoleQuery { Id = Id });
        if (roleResult.Succeeded)
        {
            ViewBag.RoleName = roleResult.Data.Name;
        }

        var allClients = await _dbContext.Clients.AsNoTracking().ToListAsync();

        ViewBag.Clients = allClients;

        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Roles.Create)]
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
            query = query.Where(t => (t.Subject != null && t.Subject.Contains(request.SearchValue)) || (t.ReferenceId != null && t.ReferenceId.Contains(request.SearchValue)));
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

    [Authorize(Policy = Permissions.Tenant.View)]
    public async Task<IActionResult> ExportTenants()
    {
        var result = await _mediator.Send(new GetAllTenantQuery());
        var base64 = await _excelService.ExportAsync(result.Data, new Dictionary<string, Func<TenantResponse, object>>
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

    [Authorize(Policy = Permissions.Users.View)]
    public async Task<IActionResult> ExportUsers()
    {
        var result = await _mediator.Send(new SSO.Application.Features.Users.Queries.GetAll.GetAllUserQuery());
        var base64 = await _excelService.ExportAsync(result.Data, new Dictionary<string, Func<UserResponse, object>>
        {
            { "UserName", x => x.UserName },
            { "First Name", x => x.FirstName },
            { "Last Name", x => x.LastName },
            { "Email", x => x.Email },
            { "Phone", x => x.PhoneNumber ?? "" },
           // { "Tenant", x => x.TenantName ?? "" },
            { "Is Active", x => x.IsActive }
        }, "Users");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Users_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [Authorize(Policy = Permissions.Roles.View)]
    public async Task<IActionResult> ExportRoles()
    {
        var result = await _mediator.Send(new SSO.Application.Features.Roles.Queries.GetAll.GetAllRoleQuery());
        var base64 = await _excelService.ExportAsync(result.Data, new Dictionary<string, Func<RoleResponse, object>>
        {
            { "Name", x => x.Name },
            { "Description", x => x.Description ?? "" },
            { "Is System Role", x => x.IsSystemRole },
           // { "Tenant", x => x.TenantName ?? "Global" }
        }, "Roles");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Roles_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> ExportSubscriptions()
    {
        var result = await _mediator.Send(new SSO.Application.Features.Subscriptions.Queries.GetAll.GetAllSubscriptionQuery());
        var base64 = await _excelService.ExportAsync(result.Data, new Dictionary<string, Func<SubscriptionResponse, object>>
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

    [Authorize(Policy = Permissions.Client.View)]
    public async Task<IActionResult> ExportClients()
    {
        var clients = await _dbContext.Clients.AsNoTracking().ToListAsync();
        var base64 = await _excelService.ExportAsync(clients, new Dictionary<string, Func<ApplicationClient, object>>
        {
            { "Client ID", x => x.ClientId ?? "" },
            { "Display Name", x => x.DisplayName ?? "" },
            { "OIDC Type", x => x.ClientType ?? "" },
            { "App Type", x => x.AppClientType.ToString() },
            { "Is Active", x => x.IsActive }
        }, "Clients");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Clients_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [Authorize(Policy = Permissions.Templates.View)]
    public async Task<IActionResult> ExportTemplates()
    {
        var result = await _mediator.Send(new SSO.Application.Features.Templates.Queries.GetAll.GetAllTemplateQuery());
        var base64 = await _excelService.ExportAsync(result.Data, new Dictionary<string, Func<TemplateResponse, object>>
        {
            { "Name", x => x.Name },
            { "Key", x => x.Key },
            { "Type", x => x.Type.ToString() },
            { "Version", x => x.Version }
        }, "Templates");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Templates_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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
}
