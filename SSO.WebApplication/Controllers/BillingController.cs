using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Features.Invoices.Queries.GetPaged;
using SSO.Application.Features.Invoices.Queries.GetById;
using SSO.Application.Features.Payments.Commands.AddEdit;
using SSO.Application.Features.Payments.Queries.GetPaged;
using SSO.Application.Interfaces.Services;
using SSO.Application.Responses.Billing;
using SSO.Application.Requests.DataTable;
using SSO.Shared.Wrapper.Mediator;
using SSO.Application.Extensions;
using Permissions = SSO.Common.Constants.Permission.Permissions;
using SSO.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace SSO.WebApplication.Controllers;

[Authorize]
public class BillingController : Controller
{
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _dbContext;
    private readonly IExcelService _excelService;

    public BillingController(IMediator mediator, ApplicationDbContext dbContext, IExcelService excelService)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _excelService = excelService;
    }

    // ─────────────────────────────────────────────
    //  INVOICES
    // ─────────────────────────────────────────────

    [Authorize(Policy = Permissions.Subscription.View)]
    public IActionResult Invoices()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> GetInvoices()
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new GetPagedInvoicesQuery(request));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> ExportInvoices()
    {
        var data = await _dbContext.Invoices
            .Include(i => i.TenantSubscription.Tenants)
            .Include(i => i.TenantSubscription.Subscriptions)
            .AsNoTracking()
            .ToListAsync();
            
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<SSO.Domain.Entities.Invoice, object>>
        {
            { "Invoice Number", x => x.InvoiceNumber },
            { "Tenant", x => x.TenantSubscription.Tenants.Name },
            { "Subscription", x => x.TenantSubscription.Subscriptions.Name },
            { "Date", x => x.InvoiceDate.ToShortDateString() },
            { "Due Date", x => x.DueDate.ToShortDateString() },
            { "Total Amount", x => x.TotalAmount },
            { "Status", x => x.Status.ToString() }
        }, "Invoices");
        
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Invoices_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> PrintInvoice(Guid id)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id));
        if (!result.Succeeded) return NotFound();
        return View(result.Data);
    }

    // ─────────────────────────────────────────────
    //  PAYMENTS
    // ─────────────────────────────────────────────

    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> Payments(Guid? InvoiceId)
    {
        ViewBag.InvoiceId = InvoiceId;
        return View();
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> GetPayments(Guid? InvoiceId)
    {
        var request = Request.ToDataTableRequest();
        var result = await _mediator.Send(new GetPagedPaymentsQuery(request, InvoiceId));

        return Json(new
        {
            draw = result.Draw,
            recordsTotal = result.RecordsTotal,
            recordsFiltered = result.RecordsFiltered,
            data = result.Data
        });
    }

    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> ExportPayments()
    {
        var data = await _dbContext.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.TenantSubscription)
                    .ThenInclude(ts => ts.Tenants)
            .AsNoTracking()
            .ToListAsync();

        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<SSO.Domain.Entities.Payment, object>>
        {
            { "Transaction ID", x => x.TransactionId },
            { "Invoice #", x => x.Invoice.InvoiceNumber },
            { "Tenant", x => x.Invoice.TenantSubscription.Tenants.Name },
            { "Amount", x => x.Amount },
            { "Method", x => x.Method.ToString() },
            { "Payment Date", x => x.PaymentDate.ToShortDateString() },
            { "Status", x => x.Status.ToString() }
        }, "Payments");

        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Payments_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> RecordPayment(Guid invoiceId)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(invoiceId));
        if (!result.Succeeded) return NotFound();
        return PartialView("_RecordPayment", new RecordPaymentCommand { 
            InvoiceId = invoiceId, 
            Amount = result.Data.TotalAmount,
            Currency = result.Data.Currency
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return Json(result);
    }
}
