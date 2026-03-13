using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Identity;
using LuxenHotel.Models.Entities.Orders;
using LuxenHotel.Models.ViewModels;
using LuxenHotel.Models.ViewModels.Booking;
using LuxenHotel.Models.ViewModels.Orders;
using LuxenHotel.Services.Booking.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Areas.Customer.Controllers;

[Authorize]
[Area("Customer")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        UserManager<User> userManager,
        ApplicationDbContext context,
        ILogger<OrdersController> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Route("Orders/MyOrders")]
    public async Task<IActionResult> MyOrders(string sortOrder, string searchString, int? pageNumber)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID claim not found for authenticated user.");
            return Unauthorized();
        }

        var orders = await _context.Orders
                .Include(o => o.Accommodation)
                .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
                .Include(o => o.OrderCombos)
                .ThenInclude(oc => oc.Combo)
                .Where(o => o.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

        var user = await _userManager.FindByIdAsync(userId);
        ViewData["UserFullName"] = user?.FullName;

        _logger.LogInformation("Retrieved {Count} orders for user {UserId}", orders.Count, userId);

        // Return all orders and let DataTables handle the pagination
        return View(orders);
    }

    [HttpGet]
    [Route("Orders/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("User ID claim not found for authenticated user.");
            return Unauthorized();
        }

        var order = await _context.Orders
            .Include(o => o.Accommodation)
            .Include(o => o.User)
            .Include(o => o.OrderServices)
            .ThenInclude(os => os.Service)
            .Include(o => o.OrderCombos)
            .ThenInclude(oc => oc.Combo)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found or not owned by user {UserId}.", id, userId);
            return NotFound($"Order with ID {id} not found or you do not have permission to view it.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        ViewData["UserFullName"] = user?.FullName;

        var viewModel = new OrderDetailsViewModel
        {
            OrderCode = order.OrderCode,
            CustomerName = order.CustomerName ?? order.User?.FullName ?? "N/A",
            CustomerEmail = order.CustomerEmail ?? order.User?.Email ?? "N/A",
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
            CancellationReason = order.CancellationReason ?? "N/A",
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
            }).ToList(),
        };

        ViewBag.AccommodationPrice = order.Accommodation?.Price ?? 0;
        _logger.LogInformation("Retrieved details for order {OrderCode} for user {UserId}.", order.OrderCode, userId);

        return View(viewModel);
    }

    // GET: Orders/Create/5
    [HttpGet]
    [Route("Orders/Create/{accommodationId:int}")]
    public async Task<IActionResult> Create(int accommodationId)
    {
        // Verify accommodation exists
        var accommodation = await _context.Accommodations
            .FirstOrDefaultAsync(a => a.Id == accommodationId);

        if (accommodation == null)
        {
            return NotFound();
        }

        var viewModel = new OrderCreateViewModel
        {
            AccommodationId = accommodationId,
            CheckInDate = DateTime.Today.AddDays(1),
            CheckOutDate = DateTime.Today.AddDays(2),
        };

        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    viewModel.CustomerName = user.FullName;
                    viewModel.CustomerEmail = user.Email;
                    viewModel.CustomerPhone = user.PhoneNumber;
                }
                else
                {
                    // Log warning: User not found in database
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                }
            }
            else
            {
                // Log warning: User ID claim missing
                _logger.LogWarning("User ID claim not found for authenticated user.");
            }
        }

        ViewData["Accommodations"] = await _context.Accommodations
            .Where(a => a.Id == accommodationId)
            .AsNoTracking()
            .ToListAsync();
        ViewData["Services"] = await _context.Services
            .Where(s => s.AccommodationId == accommodationId)
            .ToListAsync();
        ViewData["Combos"] = await _context.Combos
            .Where(s => s.AccommodationId == accommodationId)
            .ToListAsync();

        return View(viewModel);
    }

    // POST: Orders/Create/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Orders/Create/{accommodationId:int}")]
    public async Task<IActionResult> Create(int accommodationId, OrderCreateViewModel viewModel,
        int[] selectedServiceIds, int[] serviceQuantities, int[] selectedComboIds, int[] comboQuantities)
    {
        // Ensure AccommodationId matches route parameter
        if (viewModel.AccommodationId != accommodationId)
        {
            ModelState.AddModelError("AccommodationId", "Invalid accommodation ID.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Fetch accommodation
                var accommodation = await _context.Accommodations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == accommodationId);

                if (accommodation == null)
                {
                    _logger.LogWarning("Accommodation with ID {AccommodationId} not found.", accommodationId);
                    ModelState.AddModelError("", "Accommodation not found.");
                    return View(viewModel);
                }

                // Map ViewModel to Orders entity
                var order = new Orders
                {
                    AccommodationId = viewModel.AccommodationId,
                    CustomerName = viewModel.CustomerName,
                    CustomerEmail = viewModel.CustomerEmail,
                    CustomerPhone = viewModel.CustomerPhone,
                    CheckInDate = viewModel.CheckInDate,
                    CheckOutDate = viewModel.CheckOutDate,
                    NumberOfGuests = viewModel.NumberOfGuests,
                    SpecialRequests = viewModel.SpecialRequests,
                    PaymentMethod = viewModel.PaymentMethod,
                    CreatedAt = DateTime.UtcNow,
                    Status = OrderStatus.Created,
                    PaymentStatus = PaymentStatus.Pending,
                    OrderServices = new List<OrderService>(),
                    OrderCombos = new List<OrderCombo>()
                };

                // Associate user if authenticated
                if (User.Identity.IsAuthenticated)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        order.UserId = userId;
                    }
                }

                // Track prices
                Dictionary<int, int> servicePrices = new Dictionary<int, int>();
                Dictionary<int, int> comboPrices = new Dictionary<int, int>();

                // Process services
                await ProcessOrderItems(
                    selectedServiceIds,
                    serviceQuantities,
                    order.OrderServices,
                    servicePrices,
                    id => _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id),
                    (id, quantity) => new OrderService { ServiceId = id, Quantity = quantity },
                    "service",
                    _logger
                );

                // Process combos
                await ProcessOrderItems(
                    selectedComboIds,
                    comboQuantities,
                    order.OrderCombos,
                    comboPrices,
                    id => _context.Combos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id),
                    (id, quantity) => new OrderCombo { ComboId = id, Quantity = quantity },
                    "combo",
                    _logger
                );

                // Generate order code
                order.OrderCode = Guid.NewGuid().ToString("N")[..10].ToUpper();

                // Calculate total price
                int accommodationPriceForCalculation = accommodation.Price;
                int numberOfDays = Math.Max(1, (int)Math.Ceiling((order.CheckOutDate - order.CheckInDate).TotalDays));
                int totalPrice = accommodationPriceForCalculation * numberOfDays;

                // Calculate service total
                totalPrice += CalculateOrderItemsTotal(
                    order.OrderServices,
                    servicePrices,
                    item => item.ServiceId,
                    item => item.Quantity,
                    "service",
                    _logger
                );

                // Calculate combo total
                totalPrice += CalculateOrderItemsTotal(
                    order.OrderCombos,
                    comboPrices,
                    item => item.ComboId,
                    item => item.Quantity,
                    "combo",
                    _logger
                );

                order.TotalPrice = totalPrice;
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully created order with ID {OrderId} containing {ServiceCount} services and {ComboCount} combos",
                    order.Id, order.OrderServices.Count, order.OrderCombos.Count);

                // Check if VNPay is selected and redirect to payment processor
                if (order.PaymentMethod == PaymentMethod.VNPay)
                {
                    _logger.LogInformation("Redirecting to VNPay payment gateway for order {OrderId}", order.Id);
                    return RedirectToAction("ProcessPayment", "Payment", new { area = "Customer", orderId = order.Id });
                }

                // Otherwise, show the order details
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for AccommodationId {AccommodationId}.", accommodationId);
                ModelState.AddModelError("", $"An error occurred while creating the order: {ex.Message}");
            }
        }

        // Reload dropdown data if validation fails
        ViewData["Accommodations"] = await _context.Accommodations
            .Where(a => a.Id == accommodationId)
            .AsNoTracking()
            .ToListAsync();
        ViewData["Services"] = await _context.Services
            .Where(s => s.AccommodationId == accommodationId)
            .AsNoTracking()
            .ToListAsync();
        ViewData["Combos"] = await _context.Combos
            .Where(c => c.AccommodationId == accommodationId)
            .AsNoTracking()
            .ToListAsync();

        return View(viewModel);
    }

    private int CalculateOrderItemsTotal<TOrderItem>(
        IEnumerable<TOrderItem> orderItems,
        Dictionary<int, int> prices,
        Func<TOrderItem, int> getItemId,
        Func<TOrderItem, int> getQuantity,
        string itemType,
        ILogger logger)
    {
        int total = 0;
        foreach (var item in orderItems)
        {
            int itemId = getItemId(item);
            if (prices.TryGetValue(itemId, out int itemPrice))
            {
                int quantity = getQuantity(item);
                total += quantity * itemPrice;
            }
            else
            {
                logger.LogWarning($"Price for {itemType} with ID {{ItemId}} not found.", itemId);
            }
        }

        return total;
    }

    private async Task ProcessOrderItems<TOrderItem, TEntity>(
        int[] selectedIds,
        int[] quantities,
        List<TOrderItem> orderItems,
        Dictionary<int, int> prices,
        Func<int, Task<TEntity>> fetchEntity,
        Func<int, int, TOrderItem> createOrderItem,
        string itemType,
        ILogger logger)
        where TOrderItem : class
        where TEntity : class?
    {
        if (selectedIds != null && selectedIds.Length > 0 && quantities != null && quantities.Length > 0)
        {
            logger.LogInformation($"Processing {{Count}} selected {itemType}s", selectedIds.Length);
            for (int i = 0; i < Math.Min(selectedIds.Length, quantities.Length); i++)
            {
                if (selectedIds[i] > 0 && quantities[i] > 0)
                {
                    var entity = await fetchEntity(selectedIds[i]);
                    if (entity != null)
                    {
                        var orderItem = createOrderItem(selectedIds[i], quantities[i]);
                        orderItems.Add(orderItem);
                        var priceProperty = entity.GetType().GetProperty("Price");
                        if (priceProperty != null)
                        {
                            prices[selectedIds[i]] = (int)priceProperty.GetValue(entity);
                        }

                        logger.LogInformation($"Added {itemType} {{Id}} with quantity {{Quantity}} to order",
                            selectedIds[i], quantities[i]);
                    }
                    else
                    {
                        logger.LogWarning($"{itemType} with ID {{Id}} not found.", selectedIds[i]);
                    }
                }
                else
                {
                    logger.LogWarning(
                        $"Invalid {itemType} data at index {{Index}}: Id={{Id}}, Quantity={{Quantity}}",
                        i, selectedIds[i], quantities[i]);
                }
            }
        }
        else
        {
            logger.LogInformation($"No valid {itemType}s selected or quantities provided.");
        }
    }

    // GET: Orders/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.Accommodation)
            .Include(o => o.OrderServices)
            .ThenInclude(os => os.Service)
            .Include(o => o.OrderCombos)
            .ThenInclude(oc => oc.Combo)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        // Map Orders to OrderDetailsViewModel
        var viewModel = new OrderDetailsViewModel
        {
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            AccommodationName = order.Accommodation?.Name,
            TotalPrice = order.TotalPrice,
            PaymentStatus = order.PaymentStatus.ToString(),
            OrderStatus = order.Status.ToString(),
            CheckInDate = order.CheckInDate,
            CheckOutDate = order.CheckOutDate,
            NumberOfGuests = order.NumberOfGuests,
            SpecialRequests = order.SpecialRequests,
            CancellationReason = order.CancellationReason,
            CreatedAt = order.CreatedAt,
            Services = order.OrderServices.Select(os => new OrderServiceViewModel
            {
                ServiceName = os.Service?.Name, // Assuming Service has a Name property
                Quantity = os.Quantity,
                Price = os.Service?.Price ?? 0 // Assuming Service has a Price property
            }).ToList(),
            Combos = order.OrderCombos.Select(oc => new OrderComboViewModel
            {
                ComboName = oc.Combo?.Name, // Assuming Combo has a Name property
                Quantity = oc.Quantity,
                Price = oc.Combo?.Price ?? 0 // Assuming Combo has a Price property
            }).ToList()
        };

        return View(viewModel);
    }


    // GET: Orders/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.OrderServices)
            .Include(o => o.OrderCombos)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        ViewData["Accommodations"] = _context.Accommodations.ToList();
        ViewData["Services"] = _context.Services.ToList();
        ViewData["Combos"] = _context.Combos.ToList();
        return View(order);
    }

    // POST: Orders/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Orders order, int[] selectedServiceIds, int[] serviceQuantities,
        int[] selectedComboIds, int[] comboQuantities)
    {
        if (id != order.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingOrder = await _context.Orders
                    .Include(o => o.OrderServices)
                    .Include(o => o.OrderCombos)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (existingOrder == null)
                {
                    return NotFound();
                }

                // Update basic properties
                existingOrder.CheckInDate = order.CheckInDate;
                existingOrder.CheckOutDate = order.CheckOutDate;
                existingOrder.NumberOfGuests = order.NumberOfGuests;
                existingOrder.SpecialRequests = order.SpecialRequests;
                existingOrder.CustomerName = order.CustomerName;
                existingOrder.CustomerEmail = order.CustomerEmail;
                existingOrder.CustomerPhone = order.CustomerPhone;
                existingOrder.AccommodationId = order.AccommodationId;
                existingOrder.PaymentMethod = order.PaymentMethod;
                existingOrder.Status = order.Status;
                existingOrder.PaymentStatus = order.PaymentStatus;
                existingOrder.UpdatedAt = DateTime.UtcNow;

                // Update services
                existingOrder.OrderServices.Clear();
                if (selectedServiceIds != null && selectedServiceIds.Length > 0)
                {
                    for (int i = 0; i < selectedServiceIds.Length; i++)
                    {
                        if (selectedServiceIds[i] > 0 && serviceQuantities[i] > 0)
                        {
                            var service = await _context.Services.FindAsync(selectedServiceIds[i]);
                            if (service != null)
                            {
                                existingOrder.OrderServices.Add(new OrderService
                                {
                                    ServiceId = selectedServiceIds[i],
                                    Quantity = serviceQuantities[i],
                                    Service = service
                                });
                            }
                        }
                    }
                }

                // Update combos
                existingOrder.OrderCombos.Clear();
                if (selectedComboIds != null && selectedComboIds.Length > 0)
                {
                    for (int i = 0; i < selectedComboIds.Length; i++)
                    {
                        if (selectedComboIds[i] > 0 && comboQuantities[i] > 0)
                        {
                            var combo = await _context.Combos.FindAsync(selectedComboIds[i]);
                            if (combo != null)
                            {
                                existingOrder.OrderCombos.Add(new OrderCombo
                                {
                                    ComboId = selectedComboIds[i],
                                    Quantity = comboQuantities[i],
                                    Combo = combo
                                });
                            }
                        }
                    }
                }

                // Recalculate total price
                existingOrder.Accommodation = await _context.Accommodations.FindAsync(existingOrder.AccommodationId);
                existingOrder.CalculateTotalPrice();

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(order.Id))
                {
                    return NotFound();
                }

                throw;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the order: {ex.Message}");
            }
        }

        // Reload dropdown data if validation fails
        ViewData["Accommodations"] = _context.Accommodations.ToList();
        ViewData["Services"] = _context.Services.ToList();
        ViewData["Combos"] = _context.Combos.ToList();
        return View(order);
    }

    // GET: Orders/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Accommodation)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    // POST: Orders/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"An error occurred while deleting the order: {ex.Message}");
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Orders/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string cancellationReason)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = cancellationReason;
            order.CancelledAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyOrders));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"An error occurred while cancelling the order: {ex.Message}");
            return RedirectToAction(nameof(MyOrders));
        }
    }

    private bool OrderExists(int id)
    {
        return _context.Orders.Any(e => e.Id == id);
    }
}