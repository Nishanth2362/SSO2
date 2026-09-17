using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Services;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Commands.ResetSettings
{
    public class ResetRpapSettingsCommand : IRequest<Result<RpapSettings>>
    {
    }

    internal class ResetRpapSettingsCommandHandler : IRequestHandler<ResetRpapSettingsCommand, Result<RpapSettings>>
    {
        private readonly IRpapSettingsService _settingsService;

        public ResetRpapSettingsCommandHandler(IRpapSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<Result<RpapSettings>> Handle(ResetRpapSettingsCommand request, CancellationToken cancellationToken)
        {
            return await _settingsService.ResetToDefaultsAsync();
        }
    }
}
