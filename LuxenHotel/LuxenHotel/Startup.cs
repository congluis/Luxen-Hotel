using LuxenHotel.Data;
using LuxenHotel.Configuration;
using LuxenHotel.Models.Entities.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            // Register all services using ServiceRegistration
            services.RegisterServices();

            // Configure Identity using IdentityConfiguration
            services.ConfigureIdentity();

            // Configure routing
            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });

            // Configure Logging
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Debug);
            });

            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                // app.UseExceptionHandler("/Error");
                app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected error occurred. Please try again later.");
                    });
                });
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            // Seed data
            SeedData.InitializeAsync(app.ApplicationServices).GetAwaiter().GetResult();

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapAreaControllerRoute(
                    name: "vnpayReturn",
                    areaName: "Customer",
                    pattern: "vnpay/return",
                    defaults: new { controller = "Payment", action = "PaymentReturn" });

                // Customer Area: Short route for HomeController
                endpoints.MapAreaControllerRoute(
                    name: "customer_pages",
                    areaName: "Customer",
                    pattern: "{action=Index}/{id?}",
                    defaults: new { controller = "Home" });

                // Customer Area: Default route
                endpoints.MapAreaControllerRoute(
                    name: "customer_area",
                    areaName: "Customer",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // Admin Area
                endpoints.MapAreaControllerRoute(
                    name: "admin_area",
                    areaName: "Admin",
                    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

                // Staff Area
                endpoints.MapAreaControllerRoute(
                    name: "staff_area",
                    areaName: "Staff",
                    pattern: "Staff/{controller=Task}/{action=Index}/{id?}");

                // Identity Area
                endpoints.MapAreaControllerRoute(
                    name: "identity_area",
                    areaName: "Identity",
                    pattern: "Identity/{controller=Account}/{action=Login}/{id?}");
            });
        }
    }
}