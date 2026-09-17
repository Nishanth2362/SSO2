using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Services;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SSO.Infrastructure.Services.Jobs
{
    public class InvoiceJob : IInvoiceJob
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<InvoiceJob> _logger;

        public InvoiceJob(ApplicationDbContext dbContext, ILogger<InvoiceJob> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task MarkOverdueInvoicesAsync()
        {
            _logger.LogInformation("Job started: Marking overdue invoices.");

            var today = DateTime.UtcNow.Date;
            
            var pendingInvoices = await _dbContext.Invoices
                .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate.Date < today)
                .ToListAsync();

            if (pendingInvoices.Any())
            {
                foreach (var invoice in pendingInvoices)
                {
                    invoice.Status = InvoiceStatus.Overdue;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("{Count} invoices marked as Overdue.", pendingInvoices.Count);
            }
            else
            {
                _logger.LogInformation("No invoices were found that need to be marked as overdue.");
            }

            _logger.LogInformation("Job finished: Marking overdue invoices.");
        }
    }
}
