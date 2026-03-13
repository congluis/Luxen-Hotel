using LuxenHotel.Models.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LuxenHotel.Data
{
    /// <summary>
    /// Class to seed initial data such as roles and admin user.
    /// </summary>
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

                // Apply migrations
                dbContext.Database.Migrate();

                // Define roles
                string[] roles = { "Admin", "Staff", "Customer" };

                // Seed roles
                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        var role = new Role(roleName);
                        await roleManager.CreateAsync(role);
                    }
                }

                // Seed admin user
                var adminEmail = "admin@example.com";
                if (await userManager.FindByEmailAsync(adminEmail) == null)
                {
                    var adminUser = new User
                    {
                        UserName = "admin",
                        Email = adminEmail,
                        FullName = "Admin",
                        PhoneNumber = "0123456789"
                    };

                    var result = await userManager.CreateAsync(adminUser, "adminadmin");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
        }
    }
}