using Microsoft.AspNetCore.Identity;

namespace LuxenHotel.Models.Entities.Identity;
public class User : IdentityUser
{
    public string? FullName { get; set; }
}

