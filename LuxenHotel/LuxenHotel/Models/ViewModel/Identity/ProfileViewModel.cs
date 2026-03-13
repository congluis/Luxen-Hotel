using System.ComponentModel.DataAnnotations;

namespace LuxenHotel.Models.ViewModels.Identity;

public class ProfileViewModel
{
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    // Optional: Add other profile fields you might want users to be able to edit
}