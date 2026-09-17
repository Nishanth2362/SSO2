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
    private readonly IDataTableService _dataTableService;

    public BillingController(IMediator mediator, ApplicationDbContext dbContext, IExcelService excelService, IDataTableService dataTableService)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _excelService = excelService;
        _dataTableService = dataTableService;
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

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> ExportInvoices([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _dbContext.Invoices
            .Include(i => i.TenantSubscription.Tenants)
            .Include(i => i.TenantSubscription.Subscriptions)
            .AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new
            {
                InvoiceNumber = e.InvoiceNumber,
                TenantName = e.TenantSubscription.Tenants.Name,
                SubscriptionName = e.TenantSubscription.Subscriptions.Name,
                InvoiceDate = e.InvoiceDate.ToShortDateString(),
                DueDate = e.DueDate.ToShortDateString(),
                TotalAmount = e.TotalAmount,
                Status = e.Status.ToString(),
                Id = e.Id
            },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<dynamic, object>>
        {
            { "Invoice Number", x => x.InvoiceNumber },
            { "Tenant", x => x.TenantName },
            { "Subscription", x => x.SubscriptionName },
            { "Date", x => x.InvoiceDate },
            { "Due Date", x => x.DueDate },
            { "Total Amount", x => x.TotalAmount },
            { "Status", x => x.Status }
        }, "Invoices");
        return File(Convert.FromBase64String(base64), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Invoices_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> GetInvoiceDetails(Guid id)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id));
        if (!result.Succeeded) return NotFound();
        return Json(result.Data);
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

    [HttpPost]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> ExportPayments([FromBody] DataTableRequest request)
    {
        request ??= new DataTableRequest();
        var query = _dbContext.Payments
            .Include(p => p.Invoice)
                .ThenInclude(i => i.TenantSubscription)
                    .ThenInclude(ts => ts.Tenants)
            .AsNoTracking();
        var data = await _dataTableService.BuildExportAsync(
            query, request,
            e => new
            {
                TransactionId = e.TransactionId,
                InvoiceNumber = e.Invoice.InvoiceNumber,
                TenantName = e.Invoice.TenantSubscription.Tenants.Name,
                Amount = e.Amount,
                Method = e.Method.ToString(),
                PaymentDate = e.PaymentDate.ToShortDateString(),
                Status = e.Status.ToString(),
                Id = e.Id
            },
            "Id");
        var base64 = await _excelService.ExportAsync(data, new Dictionary<string, Func<dynamic, object>>
        {
            { "Transaction ID", x => x.TransactionId },
            { "Invoice #", x => x.InvoiceNumber },
            { "Tenant", x => x.TenantName },
            { "Amount", x => x.Amount },
            { "Method", x => x.Method },
            { "Payment Date", x => x.PaymentDate },
            { "Status", x => x.Status }
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

    [HttpGet]
    [Authorize(Policy = Permissions.Subscription.View)]
    public async Task<IActionResult> GetPaymentDetails(Guid id)
    {
        var payment = await _dbContext.Payments
            .Include(p => p.Invoice)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();
        
        return Json(new {
            id = payment.Id,
            transactionId = payment.TransactionId,
            invoiceNumber = payment.Invoice?.InvoiceNumber,
            amount = payment.Amount,
            methodName = payment.Method.ToString(),
            paymentDate = payment.PaymentDate,
            status = (int)payment.Status
        });
    }
}
