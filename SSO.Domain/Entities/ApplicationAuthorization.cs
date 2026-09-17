using OpenIddict.EntityFrameworkCore.Models;

namespace SSO.Domain.Entities
{
    public class ApplicationAuthorization
    : OpenIddictEntityFrameworkCoreAuthorization<Guid, ApplicationClient, ApplicationToken>
    {
        public Guid? TenantId { get; set; }
    }
}
