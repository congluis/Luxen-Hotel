// using System.ComponentModel.DataAnnotations;
// using LuxenHotel.Models.Entities.Orders;


// public class UserListViewModel
// {
//     public string Id { get; set; }

//     [Display(Name = "User Name")]
//     public string UserName { get; set; }

//     public string Email { get; set; }

//     [Display(Name = "Phone Number")]
//     public string PhoneNumber { get; set; }

//     [Display(Name = "Full Name")]
//     public string FullName { get; set; }

//     public string Roles { get; set; }
// }

// public class UserDetailsViewModel
// {
//     public string Id { get; set; }

//     [Display(Name = "User Name")]
//     public string UserName { get; set; }

//     public string Email { get; set; }

//     [Display(Name = "Phone Number")]
//     public string PhoneNumber { get; set; }

//     [Display(Name = "Full Name")]
//     public string FullName { get; set; }

//     public List<string> Roles { get; set; }

//     public List<Orders> Orders { get; set; }
// }

// public class CreateUserViewModel
// {
//     [Required]
//     [EmailAddress]
//     public string Email { get; set; }

//     [Required]
//     [DataType(DataType.Password)]
//     [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
//     public string Password { get; set; }

//     [DataType(DataType.Password)]
//     [Display(Name = "Confirm password")]
//     [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
//     public string ConfirmPassword { get; set; }

//     [Phone]
//     [Display(Name = "Phone Number")]
//     public string PhoneNumber { get; set; }

//     [Required]
//     [Display(Name = "Full Name")]
//     public string FullName { get; set; }

//     [Display(Name = "Roles")]
//     public List<string> SelectedRoles { get; set; } = new List<string>();
// }

// public class EditUserViewModel
// {
//     public string Id { get; set; }

//     [Required]
//     [EmailAddress]
//     public string Email { get; set; }

//     [DataType(DataType.Password)]
//     [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
//     [Display(Name = "New Password (leave blank to keep current)")]
//     public string Password { get; set; }

//     [DataType(DataType.Password)]
//     [Display(Name = "Confirm password")]
//     [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
//     public string ConfirmPassword { get; set; }

//     [Phone]
//     [Display(Name = "Phone Number")]
//     public string PhoneNumber { get; set; }

//     [Required]
//     [Display(Name = "Full Name")]
//     public string FullName { get; set; }

//     [Display(Name = "Roles")]
//     public List<string> SelectedRoles { get; set; } = new List<string>();
// }