using OpenIddict.EntityFrameworkCore.Models;

namespace SSO.Domain.Entities
{
    public class ApplicationToken
    : OpenIddictEntityFrameworkCoreToken<Guid, ApplicationClient, ApplicationAuthorization>
    {
        public Guid? TenantId { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
