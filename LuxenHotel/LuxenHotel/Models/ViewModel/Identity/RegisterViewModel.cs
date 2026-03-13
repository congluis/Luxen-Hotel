using System.ComponentModel.DataAnnotations;

namespace LuxenHotel.Models.ViewModels.Identity;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter a username.")]
    [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an email address.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a password.")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
    [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Full name can only contain letters and spaces.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(12, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 10)]
    [RegularExpression(@"^(?:\+84|0)\d{9}$", ErrorMessage = "Invalid phone number. Use 10 digits starting with 0 or +84 followed by 9 digits.")]
    [Display(Name = "Phone Number")]
    [Phone(ErrorMessage = "Invalid phone number.")]
    public string? PhoneNumber { get; set; }
}