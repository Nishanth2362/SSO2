using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.Storage;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSO.WebApplication.Controllers
{
    [Authorize(Policy = Permissions.Hangfire.View)]
    public class JobsController : Controller
    {
        private readonly IInvoiceJob _invoiceJob;
        private readonly ISystemMaintenanceJob _systemJob;
        private readonly IRecurringJobManager _recurringJobManager;

        public JobsController(IInvoiceJob invoiceJob, ISystemMaintenanceJob systemJob, IRecurringJobManager recurringJobManager)
        {
            _invoiceJob = invoiceJob;
            _systemJob = systemJob;
            _recurringJobManager = recurringJobManager;
        }

        public IActionResult Index()
        {
            var jobs = new List<JobViewModel>();
            var api = JobStorage.Current.GetMonitoringApi();
            var statistics = api.GetStatistics();

            using (var connection = JobStorage.Current.GetConnection())
            {
                var storageJobs = connection.GetRecurringJobs();
                foreach (var sj in storageJobs)
                {
                    jobs.Add(new JobViewModel
                    {
                        Id = sj.Id,
                        Name = sj.Id.Replace("-", " ").Replace("_", " "),
                        Description = sj.Job?.Method?.Name ?? "Custom scheduled task.",
                        CronExpression = sj.Cron,
                        IsRunning = true,
                        CreatedAt = sj.CreatedAt,
                        LastExecution = sj.LastExecution,
                        NextExecution = sj.NextExecution
                    });
                }
            }

            // Ensure our default jobs are listed even if not yet in storage (but show as inactive)
            if (!jobs.Any(x => x.Id == "mark-overdue-invoices"))
            {
                jobs.Add(new JobViewModel
                {
                    Id = "mark-overdue-invoices",
                    Name = "Mark Overdue Invoices",
                    Description = "Checks for all Pending invoices past their Due Date and marks them as Overdue.",
                    CronExpression = "0 0 * * *",
                    IsRunning = false
                });
            }

            if (!jobs.Any(x => x.Id == "system-cleanup"))
            {
                jobs.Add(new JobViewModel
                {
                    Id = "system-cleanup",
                    Name = "System Cleanup",
                    Description = "Automatically clears temporary upload chunks and expired server logs to reclaim space.",
                    CronExpression = "0 1 * * *",
                    IsRunning = false
                });
            }

            if (!jobs.Any(x => x.Id == "data-backup"))
            {
                jobs.Add(new JobViewModel
                {
                    Id = "data-backup",
                    Name = "Data Backup",
                    Description = "Performs a full archival of tenant metadata and identity certificates to the secure vault.",
                    CronExpression = "0 3 * * *",
                    IsRunning = false
                });
            }

            // High-level statistics
            ViewBag.EnqueuedCount = statistics.Enqueued;
            ViewBag.ProcessingCount = statistics.Processing;
            ViewBag.FailedCount = statistics.Failed;
            ViewBag.SucceededCount = statistics.Succeeded;
            ViewBag.DeletedCount = statistics.Deleted;
            ViewBag.DelayedCount = statistics.Scheduled; // Hangfire 'Scheduled' means one-off delayed
            ViewBag.RecurringCount = jobs.Count(x => x.IsRunning);
            ViewBag.TotalServices = jobs.Count;

            return View(jobs);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Hangfire.Manage)]
        public IActionResult CreateCustom(string jobId, BackgroundJobType jobType, string cron)
        {
            if (string.IsNullOrEmpty(jobId) || string.IsNullOrEmpty(cron))
            {
                return Json(new { succeeded = false, message = "Job ID and CRON expression are required." });
            }

            // Validate Job ID format (Alpha-numeric, hyphens, no spaces)
            if (!System.Text.RegularExpressions.Regex.IsMatch(jobId, @"^[a-zA-Z0-9-]+$"))
            {
                return Json(new { succeeded = false, message = "Job ID can only contain letters, numbers, and hyphens (no spaces)." });
            }

            try
            {
                switch (jobType)
                {
                    case BackgroundJobType.InvoiceOverdue:
                        _recurringJobManager.AddOrUpdate<IInvoiceJob>(jobId, x => x.MarkOverdueInvoicesAsync(), cron);
                        break;
                    case BackgroundJobType.SystemCleanup:
                        _recurringJobManager.AddOrUpdate<ISystemMaintenanceJob>(jobId, x => x.CleanTemporaryFilesAsync(), cron);
                        break;
                    case BackgroundJobType.DataBackup:
                        _recurringJobManager.AddOrUpdate<ISystemMaintenanceJob>(jobId, x => x.PerformSystemBackupAsync(), cron);
                        break;
                    default:
                        return Json(new { succeeded = false, message = "The selected job logic is not yet implemented." });
                }

                return Json(new { succeeded = true, message = $"Service '{jobId}' registered successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { succeeded = false, message = $"Failed to register service: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Hangfire.Manage)]
        public IActionResult Trigger(string jobId)
        {
            // Try to find the job in storage to know what to trigger
            using (var connection = JobStorage.Current.GetConnection())
            {
                var sj = connection.GetRecurringJobs().FirstOrDefault(x => x.Id == jobId);
                if (sj != null && sj.Job != null)
                {
                    // Trigger the existing job registered in Hangfire
                    RecurringJob.TriggerJob(jobId);
                    return Json(new { succeeded = true, message = "Job enqueued successfully via Hangfire." });
                }
            }

            // Fallback for hardcoded defaults or un-registered jobs
            switch (jobId)
            {
                case "mark-overdue-invoices":
                    BackgroundJob.Enqueue<IInvoiceJob>(x => x.MarkOverdueInvoicesAsync());
                    break;
                case "system-cleanup":
                case "system-cleanup-default":
                    BackgroundJob.Enqueue<ISystemMaintenanceJob>(x => x.CleanTemporaryFilesAsync());
                    break;
                case "data-backup":
                case "data-backup-default":
                    BackgroundJob.Enqueue<ISystemMaintenanceJob>(x => x.PerformSystemBackupAsync());
                    break;
                default:
                    return Json(new { succeeded = false, message = "Job not found in storage or defaults." });
            }

            return Json(new { succeeded = true, message = "Default job enqueued successfully." });
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Hangfire.Manage)]
        public IActionResult Schedule(string jobId, string cron)
        {
            // Default handling
            if (jobId == "mark-overdue-invoices")
            {
                _recurringJobManager.AddOrUpdate<IInvoiceJob>(jobId, x => x.MarkOverdueInvoicesAsync(), cron ?? Cron.Daily());
                return Json(new { succeeded = true, message = "Job scheduled successfully." });
            }

            // For custom jobs, we might need to know the type, but if it's already in storage, we can just update its cron
            using (var connection = JobStorage.Current.GetConnection())
            {
                var sj = connection.GetRecurringJobs().FirstOrDefault(x => x.Id == jobId);
                if (sj != null && sj.Job != null)
                {
                    // Re-register with new cron
                    _recurringJobManager.AddOrUpdate(jobId, sj.Job, cron);
                    return Json(new { succeeded = true, message = "Job schedule updated." });
                }
            }

            return Json(new { succeeded = false, message = "Job not found." });
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Hangfire.Manage)]
        public IActionResult Remove(string jobId)
        {
            _recurringJobManager.RemoveIfExists(jobId);
            return Json(new { succeeded = true, message = "Recurring job removed." });
        }

        private bool IsJobActive(string jobId)
        {
            using (var connection = JobStorage.Current.GetConnection())
            {
                return connection.GetRecurringJobs().Any(x => x.Id == jobId);
            }
        }
    }

    public enum BackgroundJobType
    {
        InvoiceOverdue = 1,
        SystemCleanup = 2,
        DataBackup = 3
    }

    public class JobViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CronExpression { get; set; }
        public bool IsRunning { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
    }
}
