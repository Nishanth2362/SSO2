using SSO.Application.Interfaces.Services.Features;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Scopes.Commands.Import
{
    public class ImportScopesCommand : IRequest<IResult<ImportScopesResponse>>
    {
        public Stream Data { get; set; }
        public Guid? ClientId { get; set; }
    }

    internal class ImportScopesCommandHandler : IRequestHandler<ImportScopesCommand, IResult<ImportScopesResponse>>
    {
        private readonly IScopeServices _scopeServices;

        public ImportScopesCommandHandler(IScopeServices scopeServices)
        {
            _scopeServices = scopeServices;
        }

        public async Task<IResult<ImportScopesResponse>> Handle(ImportScopesCommand request, CancellationToken cancellationToken)
        {
            return await _scopeServices.ImportScopesAsync(request.Data, request.ClientId);
        }
    }
}
