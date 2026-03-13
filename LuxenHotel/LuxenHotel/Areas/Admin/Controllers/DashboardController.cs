using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.Entities.Orders;
using LuxenHotel.Models.ViewModels.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LuxenHotel.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ILogger<AdminBaseController> logger, ApplicationDbContext context) : base(logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            // Calculate date ranges
            DateTime today = DateTime.UtcNow.Date;
            DateTime firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            DateTime nextMonth = firstDayOfMonth.AddMonths(1);
            DateTime firstDayOfLastMonth = firstDayOfMonth.AddMonths(-1);
            DateTime lastDayOfLastMonth = firstDayOfMonth.AddDays(-1);

            // Fetch orders for current and last month in a single query
            var orders = await _context.Orders
                .Include(o => o.Accommodation)
                .Include(o => o.OrderServices).ThenInclude(os => os.Service)
                .Include(o => o.OrderCombos).ThenInclude(oc => oc.Combo)
                .Where(o => o.CreatedAt >= firstDayOfLastMonth && o.CreatedAt < nextMonth)
                .ToListAsync();

            var ordersThisMonth = orders.Where(o => o.CreatedAt >= firstDayOfMonth).ToList();
            var ordersLastMonth = orders.Where(o => o.CreatedAt < firstDayOfMonth).ToList();

            // Calculate earnings using in-memory data
            decimal expectedEarningsThisMonth = ordersThisMonth
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Created &&  o.CreatedAt >= firstDayOfMonth)
                .Sum(o => o.TotalPrice);
            decimal expectedEarningsLastMonth = ordersLastMonth
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Created &&  o.CreatedAt < firstDayOfMonth)
                .Sum(o => o.TotalPrice);

            // Calculate sales for both months
            decimal salesThisMonth = ordersThisMonth
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalPrice);
            decimal salesLastMonth = ordersLastMonth
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalPrice);

            // Calculate average daily sales
            decimal averageDailySalesThisMonth = CalculateAverageDailySales(ordersThisMonth, firstDayOfMonth, today);
            decimal averageDailySalesLastMonth =
                CalculateAverageDailySales(ordersLastMonth, firstDayOfLastMonth, lastDayOfLastMonth);

            // Fetch earnings by status in a single query
            var earningsByStatus = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Total = g.Sum(o => o.TotalPrice) })
                .ToDictionaryAsync(g => g.Status, g => g.Total);

            // Create dashboard view model
            var dashboardViewModel = new DashboardViewModel
            {
                ExpectedEarnings = expectedEarningsThisMonth,
                ExpectedEarningsChangePercentage =
                    CalculateEarningsChangePercentage(expectedEarningsThisMonth, expectedEarningsLastMonth),
                ConfirmedEarnings = earningsByStatus.GetValueOrDefault(OrderStatus.Confirmed, 0),
                InProgressEarnings = earningsByStatus.GetValueOrDefault(OrderStatus.InProgress, 0),
                CompletedEarnings = earningsByStatus.GetValueOrDefault(OrderStatus.Completed, 0),
                SalesThisMonth = salesThisMonth,
                SalesThisMonthChangePercentage = CalculateEarningsChangePercentage(salesThisMonth, salesLastMonth),
                OrdersThisMonth = ordersThisMonth.Count,
                OrdersLastMonth = ordersLastMonth.Count,
                OrdersChangePercentage = CalculateOrdersChangePercentage(ordersThisMonth.Count, ordersLastMonth.Count),
                AverageDailySales = averageDailySalesThisMonth,
                AverageDailySalesChangePercentage =
                    CalculateAverageDailySalesChangePercentage(averageDailySalesThisMonth, averageDailySalesLastMonth),
                AccommodationOrders = await GetAccommodationOrders(orders),
                RecentOrders = await GetRecentOrders()
            };

            return View(dashboardViewModel);
        }

        private static double CalculateAverageDailySalesChangePercentage(decimal currentSales, decimal previousSales)
        {
            if (previousSales == 0)
                return currentSales > 0 ? 100.0 : 0.0;
            return (double)((currentSales - previousSales) / previousSales) * 100.0;
        }

        private static double CalculateEarningsChangePercentage(decimal currentEarnings, decimal previousEarnings)
        {
            if (previousEarnings == 0)
                return currentEarnings > 0 ? 100.0 : 0.0;
            return (double)((currentEarnings - previousEarnings) / previousEarnings) * 100.0;
        }

        private static decimal CalculateAverageDailySales(IEnumerable<Orders> orders, DateTime startDate,
            DateTime endDate)
        {
            int daysElapsed = (int)(endDate - startDate).TotalDays + 1;
            if (daysElapsed == 0)
                return 0;

            decimal totalSales = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalPrice);
            return totalSales / daysElapsed;
        }

        private static double CalculateOrdersChangePercentage(int currentMonthOrders, int lastMonthOrders)
        {
            if (lastMonthOrders == 0)
                return currentMonthOrders > 0 ? 100.0 : 0.0;
            return ((currentMonthOrders - lastMonthOrders) / (double)lastMonthOrders) * 100.0;
        }

        private async Task<List<AccommodationOrderViewModel>> GetAccommodationOrders(IEnumerable<Orders> orders)
        {
            var accommodations = await _context.Accommodations
                .Include(a => a.Services)
                .ToListAsync();

            var result = accommodations.Select(accommodation =>
                {
                    var accommodationOrders = orders
                        .Where(o => o.AccommodationId == accommodation.Id)
                        .ToList();

                    var completedOrders = accommodationOrders
                        .Where(o => o.Status == OrderStatus.Completed)
                        .ToList();

                    return new AccommodationOrderViewModel
                    {
                        Accommodation = accommodation,
                        TotalOrders = completedOrders.Count,
                        Revenue = completedOrders.Sum(o => o.TotalPrice),
                        Orders = accommodationOrders
                    };
                })
                .OrderByDescending(ao => ao.Revenue)
                .ToList();

            return result;
        }

        private async Task<List<RecentOrderViewModel>> GetRecentOrders(int count = 10)
        {
            // Get most recent orders with all necessary related data
            var recentOrders = await _context.Orders
                .Include(o => o.Accommodation)
                .Include(o => o.User)
                .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
                .Include(o => o.OrderCombos)
                .ThenInclude(oc => oc.Combo)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .ToListAsync();

            var result = new List<RecentOrderViewModel>();

            foreach (var order in recentOrders)
            {
                // Calculate time elapsed since order creation
                TimeSpan elapsed = DateTime.UtcNow - order.CreatedAt;
                string timeAgo;

                if (elapsed.TotalMinutes < 60)
                    timeAgo = $"{Math.Floor(elapsed.TotalMinutes)} minutes ago";
                else if (elapsed.TotalHours < 24)
                    timeAgo = $"{Math.Floor(elapsed.TotalHours)} hours ago";
                else
                    timeAgo = $"{Math.Floor(elapsed.TotalDays)} days ago";

                // Calculate services and combos totals
                int serviceTotal = order.OrderServices.Sum(os => os.Service.Price * os.Quantity);
                int comboTotal = order.OrderCombos.Sum(oc => oc.Combo.Price * oc.Quantity);
                int accommodationTotal = order.TotalPrice - serviceTotal - comboTotal;

                // Create view model
                var orderVM = new RecentOrderViewModel
                {
                    Order = order,
                    TimeAgo = timeAgo,
                    UserName = !string.IsNullOrEmpty(order.CustomerName)
                        ? order.CustomerName
                        : order.User?.UserName ?? "Guest",
                    AccommodationTotal = accommodationTotal,
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

                result.Add(orderVM);
            }

            return result;
        }
    }

    public class DashboardViewModel
    {
        public decimal ExpectedEarnings { get; set; }
        public decimal AverageDailySales { get; set; }
        public decimal SalesThisMonth { get; set; }
        public double SalesThisMonthChangePercentage { get; set; }
        public int OrdersThisMonth { get; set; }
        public int OrdersLastMonth { get; set; }
        public double OrdersChangePercentage { get; set; }
        public double ExpectedEarningsChangePercentage { get; set; }
        public double AverageDailySalesChangePercentage { get; set; }
        public decimal ConfirmedEarnings { get; set; }
        public decimal InProgressEarnings { get; set; }
        public decimal CompletedEarnings { get; set; }
        public List<AccommodationOrderViewModel> AccommodationOrders { get; set; } = new();
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
    }

    public class AccommodationOrderViewModel
    {
        public Accommodation Accommodation { get; set; }
        public int TotalOrders { get; set; }
        public decimal Revenue { get; set; }
        public List<Orders> Orders { get; set; } = new();
    }

    public class RecentOrderViewModel
    {
        public Orders Order { get; set; }
        public string TimeAgo { get; set; }
        public string UserName { get; set; }
        public int AccommodationTotal { get; set; }

        public List<OrderServiceViewModel> Services { get; set; } = new();
        public List<OrderComboViewModel> Combos { get; set; } = new();
    }
}