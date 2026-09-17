using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSO.Application.Extensions;
using SSO.Application.Features.ManagementTransactions.Commands.ResetSettings;
using SSO.Application.Features.ManagementTransactions.Commands.Revoke;
using SSO.Application.Features.ManagementTransactions.Commands.UpdateSettings;
using SSO.Application.Features.ManagementTransactions.Queries.GetPaged;
using SSO.Application.Features.ManagementTransactions.Queries.GetSettings;
using SSO.Application.Features.ManagementTransactions.Queries.GetSummary;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;
using SSO.Infrastructure.Contexts;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers
{
    [Authorize]
    public class RestrictedAccessController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public RestrictedAccessController(
            IMediator mediator,
            ApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _mediator = mediator;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        [HttpGet("~/RestrictedAccess")]
        [Authorize(Policy = Permissions.RestrictedAccess.View)]
        public async Task<IActionResult> Index()
        {
            var summaryResult = await _mediator.Send(new GetManagementSummaryQuery());
            ViewBag.Summary = summaryResult.Data;

            var isMasterTenant = _currentUserService.IsMasterTenant;
            var tenantId = _currentUserService.TenantId;

            ViewBag.IsMasterTenant = isMasterTenant;

            var clients = isMasterTenant
                ? await _dbContext.Clients.AsNoTracking().ToListAsync()
                : await _dbContext.Clients.AsNoTracking().Where(c => c.TenantClients.Any(tc => tc.TenantId == tenantId)).ToListAsync();
            ViewBag.Clients = clients;

            var tenants = isMasterTenant
                ? await _dbContext.Tenants.AsNoTracking().ToListAsync()
                : await _dbContext.Tenants.AsNoTracking().Where(t => t.Id == tenantId).ToListAsync();
            ViewBag.Tenants = tenants;

            return View();
        }

        [HttpPost("~/RestrictedAccess/GetTransactions")]
        [Authorize(Policy = Permissions.RestrictedAccess.View)]
        public async Task<IActionResult> GetTransactions()
        {
            var request = Request.ToDataTableRequest();
            var result = await _mediator.Send(new GetPagedManagementTransactionsQuery(request));

            return Json(new
            {
                draw = result.Draw,
                recordsTotal = result.RecordsTotal,
                recordsFiltered = result.RecordsFiltered,
                data = result.Data
            });
        }

        [HttpPost("~/RestrictedAccess/RevokeSession")]
        [Authorize(Policy = Permissions.RestrictedAccess.Revoke)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSession([FromBody] RevokeRequest model)
        {
            if (model == null || model.TransactionId == Guid.Empty)
            {
                return Json(new { succeeded = false, message = "Invalid transaction ID." });
            }

            var result = await _mediator.Send(new RevokeManagementTransactionCommand
            {
                TransactionId = model.TransactionId,
                Reason = model.Reason
            });

            return Json(result);
        }

        [HttpGet("~/RestrictedAccess/GetSettings")]
        [Authorize(Policy = Permissions.RestrictedAccess.View)]
        public async Task<IActionResult> GetSettings()
        {
            var result = await _mediator.Send(new GetRpapSettingsQuery());
            return Json(result);
        }

        [HttpPost("~/RestrictedAccess/UpdateSettings")]
        [Authorize(Policy = Permissions.RestrictedAccess.Manage)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateRpapSettingsCommand command)
        {
            if (command == null)
            {
                return Json(new { succeeded = false, message = "Invalid settings payload." });
            }

            var result = await _mediator.Send(command);
            return Json(result);
        }

        [HttpPost("~/RestrictedAccess/ResetSettings")]
        [Authorize(Policy = Permissions.RestrictedAccess.Manage)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetSettings()
        {
            var result = await _mediator.Send(new ResetRpapSettingsCommand());
            return Json(result);
        }

        public class RevokeRequest
        {
            public Guid TransactionId { get; set; }
            public string? Reason { get; set; }
        }
    }
}
