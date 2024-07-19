using System.ComponentModel.DataAnnotations;

namespace Nader.OpenIddict.PasswordCredentials.Contracts;

    public record CreateUserRequest(
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            string Email,
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            string Password,
            [Display(Name = "Role")]
            string? Role
        );
