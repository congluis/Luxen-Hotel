using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace LuxenHotel.Configuration
{
    public static class IdentityConfiguration
    {
        public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
        {
            // Configure Identity
            services.AddIdentity<User, Role>(options =>
            {
                // Configure Identity options
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                // Disable features we don't need
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
                options.Lockout.AllowedForNewUsers = false;
            })
            // .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
            // .AddUserStore<UserStore<User, Role, ApplicationDbContext, string>>();

            // Configure authentication cookies
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/identity/login";
                // options.AccessDeniedPath = "/Account/AccessDenied";
            });

            // Configure cookie authentication options
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            return services;
        }
    }
}