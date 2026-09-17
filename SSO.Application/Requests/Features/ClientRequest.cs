using System.ComponentModel.DataAnnotations;
using SSO.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Requests.Features
{
    public record ClientRequest
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Client ID is required.")]
        [MaxLength(100, ErrorMessage = "Client ID cannot exceed 100 characters.")]
        public string ClientId { get; set; }

        [Required(ErrorMessage = "Client name is required.")]
        [MaxLength(200, ErrorMessage = "Client name cannot exceed 200 characters.")]
        public string ClientName { get; set; }
        public ClientType ClientType { get; set; } // spa | web | native | m2m
        public string? ClientSecret { get; set; }
        public List<string> RedirectUris { get; set; } = new();
        public List<string> PostLogoutRedirectUris { get; set; } = new();
        public List<ScopeRequest> Scopes { get; set; }= new List<ScopeRequest>();
        public string? Audience { get; set; }
    }

    public record Permissions
    {
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
