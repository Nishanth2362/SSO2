using SSO.Application.Requests.Features;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services.Features
{
    public interface IScopeServices
    {
        Task<Guid> CreateScopeAsync(ScopeRequest req, Guid? clientId = null, List<string>? skippedPermissions = null, List<string>? preExistingPermissionCodes = null);
        Task<IResult<ImportScopesResponse>> ImportScopesAsync(Stream data, Guid? clientId = null);
    }
}
