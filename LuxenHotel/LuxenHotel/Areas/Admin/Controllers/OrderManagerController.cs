using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Orders;
using LuxenHotel.Models.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Areas.Admin.Controllers;

[Route("admin/orders")]
public class OrderManagerController : AdminBaseController
{
    private readonly ApplicationDbContext _context;

    public OrderManagerController(
        ILogger<AdminBaseController> logger,
        ApplicationDbContext context
    ) : base(logger)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        SetPageTitle("Orders Management");
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Accommodation)
            .ToListAsync();
        return View(orders);
    }

    [HttpGet("{orderCode}")]
    public async Task<IActionResult> Details(string orderCode)
    {
        if (string.IsNullOrEmpty(orderCode))
        {
            return BadRequest("Order code is required.");
        }

        var order = await _context.Orders
            .Include(o => o.Accommodation)
            .Include(o => o.User)
            .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
            .Include(o => o.OrderCombos)
                .ThenInclude(oc => oc.Combo)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

        if (order == null)
        {
            return NotFound($"No order found with code {orderCode}.");
        }

        var viewModel = new OrderDetailsViewModel
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            CustomerName = order.CustomerName ?? order.User?.UserName ?? "N/A",
            CustomerEmail = order.CustomerEmail ?? "N/A",
            CustomerPhone = order.CustomerPhone ?? "N/A",
            AccommodationName = order.Accommodation?.Name ?? "N/A",
            TotalPrice = order.TotalPrice,
            PaymentStatus = order.PaymentStatus.ToString(),
            OrderStatus = order.Status.ToString(),
            CheckInDate = order.CheckInDate,
            CheckOutDate = order.CheckOutDate,
            NumberOfGuests = order.NumberOfGuests,
            SpecialRequests = order.SpecialRequests ?? "None",
            CreatedAt = order.CreatedAt,
            Services = order.OrderServices.Select(os => new OrderServiceViewModel
            {
                ServiceName = os.Service?.Name ?? "N/A",
                Quantity = os.Quantity,
                Price = os.Service?.Price ?? 0
            }).ToList(),
            Combos = order.OrderCombos.Select(oc => new OrderComboViewModel
            {
                ComboName = oc.Combo?.Name ?? "N/A",
                Quantity = oc.Quantity,
                Price = oc.Combo?.Price ?? 0
            }).ToList()
        };
        ViewBag.AccommodationPrice = order.Accommodation?.Price ?? 0;
        SetPageTitle($"Order Details - {order.OrderCode}");
        return View(viewModel);
    }

    [HttpPost("update-status/{orderId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatusAsync(int orderId, [FromForm] string targetStatus)
    {
        try
        {
            // Find the order with eager loading of related entities if needed
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["Error"] = $"Order with ID {orderId} not found.";
                return RedirectToAction("Index");
            }

            // Get current status
            var currentStatus = order.Status;

            // Parse targetStatus to OrderStatus
            if (!Enum.TryParse<OrderStatus>(targetStatus, out var newStatus) || !Enum.IsDefined(typeof(OrderStatus), newStatus))
            {
                TempData["Error"] = "Invalid status selected. Please select a valid status.";
                return RedirectToAction("Details", new { orderCode = order.OrderCode });
            }

            // No change needed if status is the same
            if (currentStatus == newStatus)
            {
                TempData["Message"] = $"Order status remains unchanged as {newStatus}.";
                return RedirectToAction("Details", new { orderCode = order.OrderCode });
            }

            // Validate status transition
            if (!IsValidStatusTransition(currentStatus, newStatus))
            {
                TempData["Error"] = $"Invalid status transition from {currentStatus} to {newStatus}.";
                return RedirectToAction("Details", new { orderCode = order.OrderCode });
            }

            // Apply business rules for specific status transitions
            var validationResult = ValidateStatusTransition(order, newStatus);
            if (!validationResult.IsValid)
            {
                TempData["Error"] = validationResult.ErrorMessage;
                return RedirectToAction("Details", new { orderCode = order.OrderCode });
            }

            // Update status within a transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update order status
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;

                // Perform additional actions based on new status
                await PerformStatusTransitionActions(order, currentStatus, newStatus);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Message"] = $"Order {order.OrderCode} status updated to {newStatus} successfully.";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "An error occurred while updating the order status. Please try again.";
            }

            return RedirectToAction("Details", new { orderCode = order.OrderCode });
        }
        catch (Exception)
        {
            TempData["Error"] = "An unexpected error occurred. Please try again or contact support.";
            return RedirectToAction("Index");
        }
    }

    private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        var allowedTransitions = new Dictionary<OrderStatus, HashSet<OrderStatus>>
    {
        { OrderStatus.Created, new HashSet<OrderStatus> { OrderStatus.Confirmed, OrderStatus.Cancelled } },
        { OrderStatus.Confirmed, new HashSet<OrderStatus> { OrderStatus.InProgress, OrderStatus.Cancelled } },
        { OrderStatus.InProgress, new HashSet<OrderStatus> { OrderStatus.Completed, OrderStatus.Cancelled } },
        { OrderStatus.Completed, new HashSet<OrderStatus>() }, // Terminal state
        { OrderStatus.Cancelled, new HashSet<OrderStatus>() }  // Terminal state
    };

        return allowedTransitions.TryGetValue(currentStatus, out var allowedStatuses) &&
               allowedStatuses.Contains(newStatus);
    }

    private (bool IsValid, string ErrorMessage) ValidateStatusTransition(Orders order, OrderStatus newStatus)
    {
        switch (newStatus)
        {
            case OrderStatus.Confirmed:
                // Add any validation rules for confirming an order
                if (order.CheckInDate < DateTime.UtcNow.Date)
                {
                    return (false, "Cannot confirm orders with a check-in date in the past.");
                }
                break;

            case OrderStatus.InProgress:
                // Validate customer has checked in
                if (order.CheckInDate > DateTime.UtcNow.Date)
                {
                    return (false, "Cannot mark as in progress before the check-in date.");
                }
                break;

            case OrderStatus.Completed:
                // Check if checkout date has passed
                if (order.CheckOutDate > DateTime.UtcNow.Date)
                {
                    return (false, "Cannot mark as completed before the check-out date.");
                }
                break;

            case OrderStatus.Cancelled:
                // Add validation for cancellation if needed
                // For example, prevent cancelling orders that are already in progress
                if (order.Status == OrderStatus.InProgress && DateTime.UtcNow > order.CheckInDate.AddDays(1))
                {
                    return (false, "Cannot cancel an order that has been in progress for more than 24 hours.");
                }
                break;
        }

        return (true, string.Empty);
    }

    private async Task PerformStatusTransitionActions(Orders order, OrderStatus oldStatus, OrderStatus newStatus)
    {
        // Perform additional actions based on the status transition
        if (newStatus == OrderStatus.Confirmed)
        {
            // Send confirmation email, update inventory, etc.
            await SendOrderConfirmationEmailAsync(order);
        }
        else if (newStatus == OrderStatus.Cancelled)
        {
            // Handle cancellation: refund processing, inventory updates, etc.
            await ProcessOrderCancellationAsync(order);
        }
        else if (newStatus == OrderStatus.Completed)
        {
            // Send thank you email, request feedback, etc.
            await ProcessOrderCompletionAsync(order);
        }
    }

    private async Task SendOrderConfirmationEmailAsync(Orders order)
    {
        // Implementation for sending confirmation email
        await Task.CompletedTask;
    }

    private async Task ProcessOrderCancellationAsync(Orders order)
    {
        // Implementation for handling cancellation
        await Task.CompletedTask;
    }

    private async Task ProcessOrderCompletionAsync(Orders order)
    {
        // Implementation for handling completion
        await Task.CompletedTask;
    }
}