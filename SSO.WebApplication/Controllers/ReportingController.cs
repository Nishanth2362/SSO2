using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSO.Application.Extensions;
using SSO.Application.Interfaces.Services;
using SSO.Common.Constants.Permission;

namespace SSO.WebApplication.Controllers
{
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
                data = data.Where(a => 
                    (a.TableName != null && a.TableName.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)) ||
                    (a.Type != null && a.Type.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)) ||
                    (a.UserId != null && a.UserId.Contains(request.SearchValue, StringComparison.OrdinalIgnoreCase)));
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

        [HttpGet]
        [Authorize(Policy = Permissions.AuditTrails.Export)]
        public async Task<IActionResult> ExportAuditLogs(string format = "excel", string searchString = "", bool searchInOldValues = false, bool searchInNewValues = false, DateTime? start = null, DateTime? end = null)
        {
            if (format == "excel")
            {
                var result = await _auditService.ExportToExcelAsync(string.Empty, searchString, searchInOldValues, searchInNewValues, start, end);
                if (result.Succeeded)
                {
                    var bytes = Convert.FromBase64String(result.Data);
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AuditLogs_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                return BadRequest(result.Messages);
            }
            else
            {
                // Fetch all data for Word or PDF
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

                if (format == "pdf_json")
                {
                    return Json(data.ToList());
                }
                
                if (format == "word")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'>");
                    sb.AppendLine("<head><meta charset='utf-8'><title>Audit Logs</title>");
                    sb.AppendLine("<style>body { font-family: Arial, sans-serif; } table { border-collapse: collapse; width: 100%; border: 1px solid #ddd; } th, td { text-align: left; padding: 8px; border: 1px solid #ddd; word-break: break-all; max-width: 200px; } th { background-color: #f2f2f2; color: #333; } </style></head>");
                    sb.AppendLine("<body><h2>System Audit Logs</h2><table border='1'>");
                    sb.AppendLine("<tr><th>Date/Time</th><th>User</th><th>Type</th><th>Table Name</th><th>Primary Key</th><th>Affected Columns</th><th>Old Values</th><th>New Values</th></tr>");
                    
                    foreach (var item in data)
                    {
                        sb.AppendLine($"<tr><td>{item.DateTime}</td><td>{item.UserId}</td><td>{item.Type}</td><td>{item.TableName}</td><td>{item.PrimaryKey}</td><td>{item.AffectedColumns}</td><td>{item.OldValues}</td><td>{item.NewValues}</td></tr>");
                    }
                    sb.AppendLine("</table></body></html>");
                    
                    return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "application/msword", $"AuditLogs_{DateTime.Now:yyyyMMddHHmmss}.doc");
                }
            }

            return BadRequest("Invalid format");
        }
    }
}
