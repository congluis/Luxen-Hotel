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

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Payment> GetPaymentByIdAsync(int id)
    {
        return await _context.Payments
            .Include(p => p.Orders)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        if (!IsPaymentValid(payment, out var errors))
        {
            throw new ValidationException(string.Join(", ", errors));
        }

        // Set default values
        payment.CreatedAt = DateTime.UtcNow;
        payment.Status = PaymentStatus.Processing;

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        return payment;
    }

    public async Task<Payment> UpdatePaymentAsync(Payment payment)
    {
        if (!IsPaymentValid(payment, out var errors))
        {
            throw new ValidationException(string.Join(", ", errors));
        }

        var existingPayment = await _context.Payments.FindAsync(payment.Id);
        if (existingPayment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {payment.Id} not found.");
        }

        _context.Entry(existingPayment).CurrentValues.SetValues(payment);
        await _context.SaveChangesAsync();

        return existingPayment;
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByOrderIdAsync(int orderId)
    {
        return await _context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Payment> ProcessPaymentAsync(int orderId, PaymentMethod method, int amount)
    {
        // Check if order exists
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            throw new KeyNotFoundException($"Order with ID {orderId} not found.");
        }

        // Create new payment
        var payment = new Payment
        {
            OrderId = orderId,
            Amount = amount,
            PaymentProvider = method.ToString(),
            Status = PaymentStatus.Processing,
            TransactionId = GenerateTransactionId(method),
            CreatedAt = DateTime.UtcNow
        };

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        // Update order payment method
        order.PaymentMethod = method;
        order.PaymentStatus = PaymentStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        return payment;
    }

    public async Task<Payment> CompletePaymentAsync(int paymentId, string transactionId, string responseData)
    {
        var payment = await GetPaymentByIdAsync(paymentId);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {paymentId} not found.");
        }

        // Update payment
        payment.Status = PaymentStatus.Completed;
        payment.TransactionId = transactionId;
        payment.ResponseData = responseData;
        payment.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update order payment status
        var order = payment.Orders;
        if (order != null)
        {
            order.PaymentStatus = PaymentStatus.Completed;
            order.TransactionId = transactionId;
            order.PaidAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // Auto-confirm if order was just created
            if (order.Status == OrderStatus.Created)
            {
                order.Status = OrderStatus.Confirmed;
            }

            await _context.SaveChangesAsync();
        }

        return payment;
    }

    public async Task<Payment> FailPaymentAsync(int paymentId, string responseData)
    {
        var payment = await GetPaymentByIdAsync(paymentId);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {paymentId} not found.");
        }

        // Update payment
        payment.Status = PaymentStatus.Failed;
        payment.ResponseData = responseData;

        await _context.SaveChangesAsync();

        // Update order payment status
        var order = payment.Orders;
        if (order != null)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return payment;
    }

    public async Task<Payment> RefundPaymentAsync(int paymentId, string responseData)
    {
        var payment = await GetPaymentByIdAsync(paymentId);
        if (payment == null)
        {
            throw new KeyNotFoundException($"Payment with ID {paymentId} not found.");
        }

        if (payment.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException($"Cannot refund payment with status {payment.Status}.");
        }

        // Update payment
        payment.Status = PaymentStatus.Refunded;
        payment.ResponseData = (payment.ResponseData ?? "") + "\nRefund: " + responseData;

        await _context.SaveChangesAsync();

        // Update order payment status
        var order = payment.Orders;
        if (order != null)
        {
            order.PaymentStatus = PaymentStatus.Refunded;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return payment;
    }

    public bool IsPaymentValid(Payment payment, out List<string> validationErrors)
    {
        validationErrors = new List<string>();

        if (payment.OrderId <= 0)
        {
            validationErrors.Add("Order ID is required.");
        }

        if (string.IsNullOrEmpty(payment.TransactionId))
        {
            validationErrors.Add("Transaction ID is required.");
        }

        if (string.IsNullOrEmpty(payment.PaymentProvider))
        {
            validationErrors.Add("Payment provider is required.");
        }

        if (payment.Amount <= 0)
        {
            validationErrors.Add("Amount must be greater than zero.");
        }

        return validationErrors.Count == 0;
    }

    public async Task<int> GetTotalPaymentsCountAsync(PaymentStatus? status = null)
    {
        var query = _context.Payments.AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }
        
        return await query.CountAsync();
    }

    public async Task<decimal> GetTotalPaymentsAmountAsync(PaymentStatus? status = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Payments.AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }
        
        if (startDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(p => status == PaymentStatus.Completed ? 
                                   p.CompletedAt <= endDate.Value : 
                                   p.CreatedAt <= endDate.Value);
        }
        
        return await query.SumAsync(p => p.Amount);
    }

    // Helper method to generate transaction ID based on payment method
    private string GenerateTransactionId(PaymentMethod method)
    {
        string prefix = method switch
        {
            PaymentMethod.VNPay => "VNP",
            PaymentMethod.Cash => "CASH",
            _ => "TXN"
        };

        return $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    }
}