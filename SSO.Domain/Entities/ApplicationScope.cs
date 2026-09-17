using OpenIddict.EntityFrameworkCore.Models;

namespace SSO.Domain.Entities
{
    public class ApplicationScope : OpenIddictEntityFrameworkCoreScope<Guid>
    {
        // business fields
        public string ScopeType { get; set; } = "Api"; // Identity / Api / Internal
        public bool IsActive { get; set; } = true;
        public bool IsSystem { get; set; }

        public ICollection<ApplicationScopePermission> Permissions { get; set; }
            = new List<ApplicationScopePermission>();
    }
}
