using SSO.Application.Interfaces.Services.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Scopes.Commands.Import
{
    public class ImportScopesCommand : IRequest<IResult<int>>
    {
        public Stream Data { get; set; }
        public Guid? ClientId { get; set; }
    }

    internal class ImportScopesCommandHandler : IRequestHandler<ImportScopesCommand, IResult<int>>
    {
        private readonly IScopeServices _scopeServices;

        public ImportScopesCommandHandler(IScopeServices scopeServices)
        {
            _scopeServices = scopeServices;
        }

        public async Task<IResult<int>> Handle(ImportScopesCommand request, CancellationToken cancellationToken)
        {
            return await _scopeServices.ImportScopesAsync(request.Data, request.ClientId);
        }
    }
}
