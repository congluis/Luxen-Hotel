using LuxenHotel.Models.Entities.Orders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LuxenHotel.Services.Order.Interfaces
{
    public interface IOrderService
    {
        // Get orders
        Task<IEnumerable<Orders>> GetAllOrdersAsync(); // Order is LuxenHotel.Models.Entities.Order.Order
        Task<Orders> GetOrderByIdAsync(int id);
        Task<Orders> GetOrderByCodeAsync(string orderCode);
        Task<IEnumerable<Orders>> GetOrdersByUserIdAsync(string userId);

        // Create and update orders
        Task<Orders> CreateOrderAsync(Orders order);
        Task<Orders> UpdateOrderAsync(Orders order);

        // Order status operations
        Task<Orders> ConfirmOrderAsync(int orderId);
        Task<Orders> StartOrderProgressAsync(int orderId);
        Task<Orders> CompleteOrderAsync(int orderId);
        Task<Orders> CancelOrderAsync(int orderId, string reason);

        // Payment related operations
        Task<Orders> UpdatePaymentStatusAsync(int orderId, PaymentStatus status,
            string transactionId); // PaymentStatus is enum

        // Additional helper methods
        Task<int> GetOrderCountAsync(OrderStatus? status = null); // OrderStatus is enum
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Validation   
        bool IsOrderValid(Orders order, out List<string> validationErrors);
    }
}