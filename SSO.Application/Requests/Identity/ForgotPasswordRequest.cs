using System.ComponentModel.DataAnnotations;

namespace SSO.Application.Requests.Identity
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
