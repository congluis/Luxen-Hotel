using Microsoft.AspNetCore.Identity;

namespace LuxenHotel.Models.Entities.Identity;


public class Role : IdentityRole
{
    public Role() : base() { }

    public Role(string roleName) : base(roleName) { }
}