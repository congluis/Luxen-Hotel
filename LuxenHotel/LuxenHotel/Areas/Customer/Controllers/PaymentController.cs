using LuxenHotel.Data;
using LuxenHotel.Helpers;
using LuxenHotel.Models.Entities.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Areas.Customer.Controllers;

[Area("Customer")]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentController> _logger;
    private readonly VnPayLibrary _vnPayLibrary;

    public PaymentController(ApplicationDbContext context, ILogger<PaymentController> logger)
    {
        _context = context;
        _logger = logger;
        _vnPayLibrary = new VnPayLibrary();
    }

    [HttpGet]
    public async Task<IActionResult> ProcessPayment(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Accommodation)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found for payment processing", orderId);
            return NotFound();
        }

        // If payment method is not VNPay, redirect back to order details
        if (order.PaymentMethod != PaymentMethod.VNPay)
        {
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }

        ViewData["Order"] = order;
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> VnPayCheckout(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Accommodation)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found for VNPay checkout", orderId);
            return NotFound();
        }

        try
        {
            // Create transaction reference
            string vnp_TxnRef = DateTime.Now.Ticks.ToString() + "-" + order.Id;

            // VNPay requires amount in VND and multiplied by 100
            string vnp_Amount = (order.TotalPrice * 100).ToString();

            // Order info to display on VNPay page
            string vnp_OrderInfo = $"Order #{order.OrderCode}";

            // Set parameters for VNPay
            _vnPayLibrary.AddRequestData("vnp_Version", VnPayConfig.Version);
            _vnPayLibrary.AddRequestData("vnp_Command", VnPayConfig.Command);
            _vnPayLibrary.AddRequestData("vnp_TmnCode", VnPayConfig.TmnCode);
            _vnPayLibrary.AddRequestData("vnp_Amount", vnp_Amount);
            _vnPayLibrary.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            _vnPayLibrary.AddRequestData("vnp_CurrCode", "VND");
            _vnPayLibrary.AddRequestData("vnp_IpAddr",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            _vnPayLibrary.AddRequestData("vnp_Locale", "vn");
            _vnPayLibrary.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
            _vnPayLibrary.AddRequestData("vnp_OrderType", VnPayConfig.OrderType);
            _vnPayLibrary.AddRequestData("vnp_ReturnUrl", VnPayConfig.ReturnUrl);
            _vnPayLibrary.AddRequestData("vnp_TxnRef", vnp_TxnRef);

            // Store transaction reference in order
            order.TransactionId = vnp_TxnRef;
            order.PaymentStatus = PaymentStatus.Processing;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Create payment URL
            string paymentUrl = _vnPayLibrary.CreateRequestUrl(VnPayConfig.PayUrl, VnPayConfig.SecretKey);

            _logger.LogInformation("Redirecting to VNPay for order {OrderId} with transaction {TransactionId}",
                order.Id, vnp_TxnRef);

            return Redirect(paymentUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay payment for order {OrderId}", order.Id);
            TempData["ErrorMessage"] = "An error occurred while processing your payment. Please try again.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
    }

    // Handles callback from VNPay
    public async Task<IActionResult> PaymentReturn()
    {
        try
        {
            // Add all query parameters to the VNPay library
            foreach (var key in Request.Query.Keys)
            {
                _vnPayLibrary.AddResponseData(key, Request.Query[key]);
            }

            // Validate the signature from VNPay
            bool isValidSignature = _vnPayLibrary.ValidateSignature(
                Request.Query["vnp_SecureHash"],
                VnPayConfig.SecretKey
            );

            if (!isValidSignature)
            {
                _logger.LogWarning("Invalid VNPay signature detected in payment return");
                TempData["ErrorMessage"] = "Invalid payment signature. Please contact support.";
                return RedirectToAction("Index", "Home");
            }

            // Parse the transaction reference to get the order ID
            string txnRef = Request.Query["vnp_TxnRef"];
            string responseCode = Request.Query["vnp_ResponseCode"];

            // Extract order ID from txnRef (format: timestamp-orderId)
            int orderId = int.Parse(txnRef.Split('-')[1]);

            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Order with ID {OrderId} not found during payment return processing", orderId);
                TempData["ErrorMessage"] = "Order not found. Please contact support.";
                return RedirectToAction("Index", "Home");
            }

            var paymentReturn = new PaymentReturnModel
            {
                TransactionId = txnRef,
                Amount = decimal.Parse(Request.Query["vnp_Amount"]) / 100,
                OrderInfo = Request.Query["vnp_OrderInfo"],
                ResponseCode = responseCode,
                IsSuccess = responseCode == "00"
            };

            // Update order based on payment status
            if (paymentReturn.IsSuccess)
            {
                order.PaymentStatus = PaymentStatus.Completed;
                order.Status = OrderStatus.Confirmed;
                order.PaidAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Payment successful for order {OrderId}", order.Id);
                TempData["SuccessMessage"] = "Payment successful! Your booking has been confirmed.";
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = "Payment failed due to user cancellation or timeout.";
                order.UpdatedAt = DateTime.UtcNow;

                _logger.LogWarning("Payment failed for order {OrderId} with response code {ResponseCode}",
                    order.Id, responseCode);
                TempData["ErrorMessage"] = "Payment was not successful. Please try again or contact support.";
            }

            await _context.SaveChangesAsync();

            return View(paymentReturn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay payment return");
            TempData["ErrorMessage"] = "An error occurred while processing your payment. Please contact support.";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CancelPayment(int orderId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found for cancellation", orderId);
            TempData["ErrorMessage"] = "Order not found. Please contact support.";
            return RedirectToAction("Index", "Home");
        }

        try
        {
            // Update order to reflect cancellation
            order.PaymentStatus = PaymentStatus.Failed;
            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = "Payment cancelled by user in payment processing stage.";
            order.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Order {OrderId} cancelled by user during payment processing. Cancellation reason: {CancellationReason}",
                order.Id, order.CancellationReason);
            TempData["ErrorMessage"] = "Payment was cancelled. Your order has been updated accordingly.";

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cancellation for order {OrderId}", order.Id);
            TempData["ErrorMessage"] = "An error occurred while cancelling your payment. Please contact support.";
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }
    }
}