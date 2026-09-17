using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Services;
using SSO.Application.Responses.Audit;
using SSO.Application.Specifications.Audit;
using SSO.Common.Wrapper;
using SSO.Domain.Models.Audit;
using SSO.Infrastructure.Contexts;
using System.Globalization;
using SSO.Application.Extensions;

namespace SSO.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDocumentService _excelService;
        private readonly IStringLocalizer<AuditService> _localizer;
        private readonly ILogger<AuditService> _logger;
        public AuditService(
            ApplicationDbContext context,
            IDocumentService excelService,
            IStringLocalizer<AuditService> localizer,
            ILogger<AuditService> logger)
        {
            _context = context;
            _excelService = excelService;
            _localizer = localizer;
            _logger = logger;
        }

        public async Task<IResult<IEnumerable<AuditResponse>>> GetCurrentUserTrailsAsync(string userId)
        {
            List<AuditResponse> trails = await _context.AuditTrails.Where(a => a.UserId == userId).OrderByDescending(a => a.Id).Take(250).Select(x => new AuditResponse()
            {
                AffectedColumns = x.AffectedColumns,
                DateTime = x.DateTime,
                Id = x.Id,
                NewValues = x.NewValues,
                OldValues = x.OldValues,
                PrimaryKey = x.PrimaryKey,
                TableName = x.TableName,
                Type = x.Type,
                UserId = x.UserId

            }).ToListAsync();
            return await Result<IEnumerable<AuditResponse>>.SuccessAsync(trails);
        }

        public async Task<IResult<IEnumerable<AuditResponse>>> GetAllTrailsAsync()
        {
            List<AuditResponse> trails = await _context.AuditTrails.OrderByDescending(a => a.Id).Take(250).Select(x => new AuditResponse()
            {
                AffectedColumns = x.AffectedColumns,
                DateTime = x.DateTime,
                Id = x.Id,
                NewValues = x.NewValues,
                OldValues = x.OldValues,
                PrimaryKey = x.PrimaryKey,
                TableName = x.TableName,
                Type = x.Type,
                UserId = x.UserId
            }).ToListAsync();
            return await Result<IEnumerable<AuditResponse>>.SuccessAsync(trails);
        }

        public async Task<IResult<string>> ExportToExcelAsync(string userId = "", string searchString = "", bool searchInOldValues = false, bool searchInNewValues = false, DateTime? start = null, DateTime? end = null, List<string>? selectedIds = null)
        {
            IQueryable<Audit> query;
            if (selectedIds != null && selectedIds.Count > 0)
            {
                var intIds = selectedIds.Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0).Where(id => id > 0).ToList();
                query = _context.AuditTrails.Where(a => intIds.Contains(a.Id));
            }
            else
            {
                AuditFilterSpecification auditSpec = new(userId, searchString, searchInOldValues, searchInNewValues, start, end);
                query = _context.AuditTrails.Specify(auditSpec);
            }

            List<Audit> trails = await query
                .OrderByDescending(a => a.DateTime)
                .ToListAsync();
            var data = await _excelService.ExportAsync("ExportAuditData", trails, CancellationToken.None);

            return await Result<string>.SuccessAsync(data: Convert.ToBase64String(data));
        }
    }
}
