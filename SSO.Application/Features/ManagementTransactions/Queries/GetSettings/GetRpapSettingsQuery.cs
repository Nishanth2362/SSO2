using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Services;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Queries.GetSettings
{
    public class GetRpapSettingsQuery : IRequest<Result<RpapSettings>>
    {
    }

    internal class GetRpapSettingsQueryHandler : IRequestHandler<GetRpapSettingsQuery, Result<RpapSettings>>
    {
        private readonly IRpapSettingsService _settingsService;

        public GetRpapSettingsQueryHandler(IRpapSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public Task<Result<RpapSettings>> Handle(GetRpapSettingsQuery request, CancellationToken cancellationToken)
        {
            var settings = _settingsService.GetSettings();
            return Task.FromResult(Result<RpapSettings>.Success(settings));
        }
    }
}
