using System;

namespace SSO.Application.Features.Users.Queries.GetByTenantName
{
    /// <summary>
    /// Lightweight user projection returned by the GetByTenantName endpoint.
    /// </summary>
    public class TenantUserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }
}
