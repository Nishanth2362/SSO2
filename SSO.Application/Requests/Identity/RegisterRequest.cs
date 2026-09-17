using System;
using System.ComponentModel.DataAnnotations;

namespace SSO.Application.Requests.Identity
{
    public class RegisterRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public string? ReturnUrl { get; set; }
        
        [Required]
        public string ClientId { get; set; }

        [Required]
        public Guid TenantId { get; set; }
    }
}
