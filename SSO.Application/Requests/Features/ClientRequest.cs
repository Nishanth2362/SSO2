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

        /// <summary>When true, all users logging into this client are required to complete an OTP step.</summary>
        public bool Require2FA { get; set; }

        /// <summary>Controls which authentication flows are shown on the login page for this client.</summary>
        public LoginMethod AllowedLoginMethod { get; set; } = LoginMethod.CredentialsOnly;

        /// <summary>When true, allows public self-registration from the login page for this client.</summary>
        public bool AllowPublicRegistration { get; set; } = false;

        /// <summary>The default role assigned to self-registered users under this client (e.g. "End User", "Student").</summary>
        public string? DefaultRoleName { get; set; } = "End User";

        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastModifiedOn { get; set; }
        public string? IPAddress { get; set; }
        public bool IsDeleted { get; set; }
    }

    public record Permissions
    {
        public string? Name { get; set; }
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
