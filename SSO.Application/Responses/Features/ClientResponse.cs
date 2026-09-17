using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Responses.Features
{
    public record ClientResponse
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string Scopes { get; set; }
        public string AppClientType { get; set; }
        public string ClientType { get; set; }
        public string? Audience { get; set; }
    }
}
