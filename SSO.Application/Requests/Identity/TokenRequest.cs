using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SSO.Application.Requests.Identity
{
    public class TokenRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Enter your email or username.")]
        [Display(Name = "Email or username")]
        public string? UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        public string? Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public int Step { get; set; } = 1;
        public string? ReturnUrl { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                yield return new ValidationResult("Enter your email or username.", new[] { nameof(UserName) });
            }

            if (Step >= 2 && string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult("Enter your password.", new[] { nameof(Password) });
            }
        }
    }
}
