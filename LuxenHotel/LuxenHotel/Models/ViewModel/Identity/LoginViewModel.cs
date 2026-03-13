using System.ComponentModel.DataAnnotations;

namespace LuxenHotel.Models.ViewModels.Identity;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your username or email.")]
    [Display(Name = "Username or Email")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}