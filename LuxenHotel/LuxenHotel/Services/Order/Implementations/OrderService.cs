
using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Orders;
using LuxenHotel.Services.Order.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LuxenHotel.Services.Order.Implementations;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IPaymentService _paymentService;

    public OrderService(ApplicationDbContext context, IPaymentService paymentService)
    {
        _context = context;
        _paymentService = paymentService;
    }

    public async Task<IEnumerable<Orders>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Include(o => o.Accommodation)
            .Include(o => o.User)
            .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
            .Include(o => o.OrderCombos)
                .ThenInclude(oc => oc.Combo)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Orders> GetOrderByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Accommodation)
            .Include(o => o.User)
            .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
            .Include(o => o.OrderCombos)
                .ThenInclude(oc => oc.Combo)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Orders> GetOrderByCodeAsync(string orderCode)
    {
        return await _context.Orders
            .Include(o => o.Accommodation)
            .Include(o => o.User)
            .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
            .Include(o => o.OrderCombos)
                .ThenInclude(oc => oc.Combo)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
    }

    public async Task<IEnumerable<Orders>> GetOrdersByUserIdAsync(string userId)
    {
        return await _context.Orders
            .Include(o => o.Accommodation)
            .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
            .Include(o => o.OrderCombos)
                .ThenInclude(oc => oc.Combo)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Orders> CreateOrderAsync(Orders order)
    {
        if (!IsOrderValid(order, out var errors))
        {
            throw new ValidationException(string.Join(", ", errors));
        }

        // Generate a unique order code if not provided
        if (string.IsNullOrEmpty(order.OrderCode))
        {
            order.OrderCode = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
        }

        // Calculate total price
        order.CalculateTotalPrice();

        // Set default values
        order.Status = OrderStatus.Created;
        order.PaymentStatus = PaymentStatus.Pending;
        order.CreatedAt = DateTime.UtcNow;

        if (order.OrderServices != null)
        {
            foreach (var os in order.OrderServices)
            {
                os.Order = null;
            }
        }

        if (order.OrderCombos != null)
        {
            foreach (var oc in order.OrderCombos)
            {
                oc.Order = null;
            }
        }

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        return order;
    }

    public async Task<Orders> UpdateOrderAsync(Orders order)
    {
        if (!IsOrderValid(order, out var errors))
        {
            throw new ValidationException(string.Join(", ", errors));
        }

        var existingOrder = await _context.Orders.FindAsync(order.Id);
        if (existingOrder == null)
        {
            throw new KeyNotFoundException($"Order with ID {order.Id} not found.");
        }

        // Recalculate total price
        order.CalculateTotalPrice();
        order.UpdatedAt = DateTime.UtcNow;

        _context.Entry(existingOrder).CurrentValues.SetValues(order);
        await _context.SaveChangesAsync();

        return existingOrder;
    }

    public async Task<Orders> ConfirmOrderAsync(int orderId)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        if (order.Status != OrderStatus.Created)
        {
            throw new InvalidOperationException($"Cannot confirm order with status {order.Status}.");
        }

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Orders> StartOrderProgressAsync(int orderId)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException($"Cannot start progress for order with status {order.Status}.");
        }

        order.Status = OrderStatus.InProgress;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Orders> CompleteOrderAsync(int orderId)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        if (order.Status != OrderStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete order with status {order.Status}.");
        }

        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Orders> CancelOrderAsync(int orderId, string reason)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel order with status {order.Status}.");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = reason;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        // If payment was completed, initiate refund process
        if (order.PaymentStatus == PaymentStatus.Completed)
        {
            order.PaymentStatus = PaymentStatus.Refunded;

            // Get the payment and update its status
            var payments = await _paymentService.GetPaymentsByOrderIdAsync(orderId);
            var payment = payments.FirstOrDefault(p => p.Status == PaymentStatus.Completed);

            if (payment != null)
            {
                await _paymentService.RefundPaymentAsync(payment.Id, "Order cancelled: " + reason);
            }
        }

        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Orders> UpdatePaymentStatusAsync(int orderId, PaymentStatus status, string transactionId)
    {
        var order = await GetOrderByIdAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        order.PaymentStatus = status;
        order.TransactionId = transactionId;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == PaymentStatus.Completed && order.PaidAt == null)
        {
            order.PaidAt = DateTime.UtcNow;

            // Auto-confirm the order if payment is completed
            if (order.Status == OrderStatus.Created)
            {
                order.Status = OrderStatus.Confirmed;
            }
        }

        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<int> GetOrderCountAsync(OrderStatus? status = null)
    {
        var query = _context.Orders.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query.CountAsync();
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Orders
            .Where(o => o.PaymentStatus == PaymentStatus.Completed);

        if (startDate.HasValue)
        {
            query = query.Where(o => o.PaidAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(o => o.PaidAt <= endDate.Value);
        }

        return await query.SumAsync(o => o.TotalPrice);
    }

    public bool IsOrderValid(Orders order, out List<string> validationErrors)
    {
        validationErrors = new List<string>();

        // Check required fields
        if (order.AccommodationId <= 0)
        {
            validationErrors.Add("Accommodation is required.");
        }

        if (order.TotalPrice < 0)
        {
            validationErrors.Add("Total price cannot be negative.");
        }

        if (order.CheckInDate >= order.CheckOutDate)
        {
            validationErrors.Add("Check-out date must be after check-in date.");
        }

        if (order.NumberOfGuests <= 0)
        {
            validationErrors.Add("Number of guests must be greater than zero.");
        }

        // Check logical validation
        if (order.OrderServices != null && order.OrderServices.Any(os => os.Quantity <= 0))
        {
            validationErrors.Add("All service quantities must be greater than zero.");
        }

        if (order.OrderCombos != null && order.OrderCombos.Any(oc => oc.Quantity <= 0))
        {
            validationErrors.Add("All combo quantities must be greater than zero.");
        }

        // Nếu không cho đặt cả Service và Combo cùng lúc
        if ((order.OrderServices?.Any() ?? false) && (order.OrderCombos?.Any() ?? false))
        {
            validationErrors.Add("Order cannot include both services and combos at the same time.");
        }

        // Either UserId or Customer info is required
        if (string.IsNullOrEmpty(order.UserId) &&
            (string.IsNullOrWhiteSpace(order.CustomerName) ||
             string.IsNullOrWhiteSpace(order.CustomerEmail) ||
             string.IsNullOrWhiteSpace(order.CustomerPhone)))
        {
            validationErrors.Add("Either user ID or full customer information (name, email, phone) must be provided.");
        }

        return validationErrors.Count == 0;
    }
}