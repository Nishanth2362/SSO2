using System;

namespace SSO.Application.Responses.Features
{
    public class PermissionResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string? Category { get; set; }
    }
}
