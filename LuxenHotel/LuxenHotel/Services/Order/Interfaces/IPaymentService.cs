
using LuxenHotel.Models.Entities.Orders;
using System.Threading.Tasks;

namespace LuxenHotel.Services.Order.Interfaces;

public interface IPaymentService
{
    // Payment operations
    Task<Payment> GetPaymentByIdAsync(int id);
    Task<Payment> CreatePaymentAsync(Payment payment);
    Task<Payment> UpdatePaymentAsync(Payment payment);
    Task<IEnumerable<Payment>> GetPaymentsByOrderIdAsync(int orderId);
    
    // Payment processing methods
    Task<Payment> ProcessPaymentAsync(int orderId, PaymentMethod method, int amount);
    Task<Payment> CompletePaymentAsync(int paymentId, string transactionId, string responseData);
    Task<Payment> FailPaymentAsync(int paymentId, string responseData);
    Task<Payment> RefundPaymentAsync(int paymentId, string responseData);
    
    // Payment validation
    bool IsPaymentValid(Payment payment, out List<string> validationErrors);
    
    // Payment statistics
    Task<int> GetTotalPaymentsCountAsync(PaymentStatus? status = null);
    Task<decimal> GetTotalPaymentsAmountAsync(PaymentStatus? status = null, DateTime? startDate = null, DateTime? endDate = null);
}