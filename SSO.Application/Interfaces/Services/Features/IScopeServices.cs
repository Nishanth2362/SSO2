using SSO.Application.Requests.Features;
using SSO.Common.Wrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services.Features
{
    public interface IScopeServices
    {
        Task<Guid> CreateScopeAsync(ScopeRequest req, Guid? clientId = null);
        Task<IResult<int>> ImportScopesAsync(Stream data, Guid? clientId = null);
    }
}
