using System;

namespace SSO.Application.Responses.Features
{
    public class RolePermissionResponse
    {
        public Guid Id { get; set; }
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public Guid PermissionId { get; set; }
        public string PermissionName { get; set; }
        public Guid ClientApplicationId { get; set; }
        public string ClientApplicationName { get; set; }
    }
}
