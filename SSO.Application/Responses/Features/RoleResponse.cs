using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Responses.Features
{
    public class RoleResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid TenantId { get; set; }
        public bool IsSystemRole { get; set; }
    }
}
