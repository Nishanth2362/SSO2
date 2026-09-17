using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Extensions;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;

namespace SSO.WebApplication.Controllers
{
    /// <summary>Request model for smart audit log export (supports SelectedIds, search, and date filters).</summary>
    public class ExportAuditRequest
    {
        public string? Format { get; set; } = "excel";
        public string? SearchValue { get; set; }
        public bool SearchInOldValues { get; set; }
        public bool SearchInNewValues { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string>? SelectedIds { get; set; }
    }

    [Authorize]
    public class ReportingController : Controller
    {
        private readonly IAuditService _auditService;

        public ReportingController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [Authorize(Policy = Permissions.AuditTrails.View)]
        public IActionResult AuditLogs()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = Permissions.AuditTrails.View)]
        public async Task<IActionResult> GetAuditLogs()
        {
            var request = Request.ToDataTableRequest();
            var result = await _auditService.GetAllTrailsAsync();

            var data = result.Data.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                bool searchOld = false;
                bool searchNew = false;
                if (request.Filters != null)
                {
                    if (request.Filters.TryGetValue("searchInOldValues", out var oldVal) && oldVal == "true")
                        searchOld = true;
                    if (request.Filters.TryGetValue("searchInNewValues", out var newVal) && newVal == "true")
                        searchNew = true;
                }

                data = data.Where(a => 
                    (a.TableName != null && a.TableName.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)) ||
                    (a.Type != null && a.Type.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)) ||
                    (a.UserId != null && a.UserId.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)) ||
                    (searchOld && a.OldValues != null && a.OldValues.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)) ||
                    (searchNew && a.NewValues != null && a.NewValues.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)));
            }

            if (request.StartDate.HasValue)
            {
                data = data.Where(a => a.DateTime >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                data = data.Where(a => a.DateTime <= endOfDay);
            }
            
            var totalRecords = result.Data.Count();
            var filteredRecords = data.Count();

            var pagedData = data.Skip(request.Start).Take(request.Length).ToList();

            return Json(new
            {
                draw = request.Draw,
                recordsTotal = totalRecords,
                recordsFiltered = filteredRecords,
                data = pagedData
            });
        }

        [HttpPost]
        [Authorize(Policy = Permissions.AuditTrails.Export)]
        public async Task<IActionResult> ExportAuditLogs([FromBody] ExportAuditRequest? exportRequest = null)
        {
            var request = exportRequest ?? new ExportAuditRequest();
            var searchString = request.SearchValue ?? string.Empty;
            var searchInOldValues = request.SearchInOldValues;
            var searchInNewValues = request.SearchInNewValues;
            var format = request.Format ?? "excel";

            if (format == "excel")
            {
                var result = await _auditService.ExportToExcelAsync(string.Empty, searchString, searchInOldValues, searchInNewValues, request.StartDate, request.EndDate, request.SelectedIds);
                if (result.Succeeded)
                {
                    var bytes = Convert.FromBase64String(result.Data);
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AuditLogs_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                return BadRequest(result.Messages);
            }
            else
            {
                var result = await _auditService.GetAllTrailsAsync();
                var data = result.Data.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    data = data.Where(a =>
                        (a.TableName != null && a.TableName.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (a.Type != null && a.Type.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (a.UserId != null && a.UserId.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (searchInOldValues && a.OldValues != null && a.OldValues.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (searchInNewValues && a.NewValues != null && a.NewValues.Contains(searchString, StringComparison.OrdinalIgnoreCase)));
                }

                if (request.StartDate.HasValue)
                    data = data.Where(a => a.DateTime >= request.StartDate.Value);
                if (request.EndDate.HasValue)
                {
                    var endOfDay = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                    data = data.Where(a => a.DateTime <= endOfDay);
                }

                // Apply SelectedIds filter
                if (request.SelectedIds != null && request.SelectedIds.Count > 0)
                    data = data.Where(a => request.SelectedIds.Contains(a.Id.ToString()));

                if (format == "pdf_json")
                    return Json(data.ToList());

                if (format == "word")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>");
                    sb.AppendLine("<head><meta charset='utf-8'><title>Audit Logs</title>");
                    sb.AppendLine("<style>body { font-family: Arial, sans-serif; } table { border-collapse: collapse; width: 100%; border: 1px solid #ddd; } th, td { text-align: left; padding: 8px; border: 1px solid #ddd; word-break: break-all; max-width: 200px; } th { background-color: #f2f2f2; color: #333; } </style></head>");
                    sb.AppendLine("<body><h2>System Audit Logs</h2><table border='1'>");
                    sb.AppendLine("<tr><th>Date/Time</th><th>User</th><th>Type</th><th>Table Name</th><th>Primary Key</th><th>Affected Columns</th><th>Old Values</th><th>New Values</th></tr>");

                    foreach (var item in data)
                        sb.AppendLine($"<tr><td>{item.DateTime}</td><td>{item.UserId}</td><td>{item.Type}</td><td>{item.TableName}</td><td>{item.PrimaryKey}</td><td>{item.AffectedColumns}</td><td>{item.OldValues}</td><td>{item.NewValues}</td></tr>");

                    sb.AppendLine("</table></body></html>");
                    return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "application/msword", $"AuditLogs_{DateTime.Now:yyyyMMddHHmmss}.doc");
                }
            }

            return BadRequest("Invalid format");
        }


        [HttpGet]
        [Authorize(Policy = Permissions.AuditTrails.View)]
        public async Task<IActionResult> GetAuditLogDetails(int id)
        {
            var result = await _auditService.GetAllTrailsAsync();
            var log = result.Data.FirstOrDefault(a => a.Id == id);
            if (log == null) return NotFound();
            return Json(log);
        }
    }
}
