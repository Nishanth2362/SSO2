using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Responses.Features
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<Guid> ClientIds { get; set; } = new List<Guid>();
        public List<string> Roles { get; set; } = new List<string>();
    }
}
