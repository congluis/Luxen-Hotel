namespace LuxenHotel.Models.ViewModels.Orders
{
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string AccommodationName { get; set; }
        public int TotalPrice { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public string SpecialRequests { get; set; }
        public string CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderServiceViewModel> Services { get; set; } = new();
        public List<OrderComboViewModel> Combos { get; set; } = new();
    }

    public class OrderServiceViewModel
    {
        public string ServiceName { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
    }

    public class OrderComboViewModel
    {
        public string ComboName { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
    }
}