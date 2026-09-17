using SSO.Application.Responses.Audit;
using SSO.Common.Wrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services
{
    public interface IAuditService
    {
        Task<IResult<IEnumerable<AuditResponse>>> GetCurrentUserTrailsAsync(string userId);
        Task<IResult<IEnumerable<AuditResponse>>> GetAllTrailsAsync();

        Task<IResult<string>> ExportToExcelAsync(string userId = "", string searchString = "", bool searchInOldValues = false, bool searchInNewValues = false, DateTime? start = null, DateTime? end = null);
    }
}
