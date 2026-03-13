// File: ServiceRegistration.cs
using LuxenHotel.Services; // Namespace chứa các service như IAccommodationService, IBookingService, ...
using Microsoft.AspNetCore.Identity;
using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.Entities.Identity;
using LuxenHotel.Services.Booking.Implementations;
using LuxenHotel.Services.Booking.Interfaces;
using LuxenHotel.Services.Identity;
using LuxenHotel.Services.Order.Implementations;
using LuxenHotel.Services.Order.Interfaces;

namespace LuxenHotel.Configuration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            // Register custom user store
            services.AddTransient<IUserStore<User>, CustomUserStore>();
            services.AddTransient<IRoleStore<Role>, CustomRoleStore>();

            // Register AccommodationService
            services.AddScoped<IAccommodationService, AccommodationService>();
            
            // Register OrderService
            services.AddScoped<IOrderService, OrderService>();

            // Register PaymentService
            services.AddScoped<IPaymentService, PaymentService>();

            // Register ComboService
            services.AddScoped<IComboService, ComboService>();

            return services;
        }
    }
}