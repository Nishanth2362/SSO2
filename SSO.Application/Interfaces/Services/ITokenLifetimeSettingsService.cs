using SSO.Application.Configuaration;
using SSO.Common.Wrapper;
using System;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface ITokenLifetimeSettingsService
    {
        Task<TokenLifetimeSettings> GetActiveSettingsAsync(Guid? tenantId = null);
        Task<Result<TokenLifetimeSettings>> UpdateSettingsAsync(TokenLifetimeSettings newSettings);
        Task<Result<TokenLifetimeSettings>> ResetToDefaultsAsync(Guid? tenantId = null);
    }
}
