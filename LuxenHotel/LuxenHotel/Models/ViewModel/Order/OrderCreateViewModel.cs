using System.ComponentModel.DataAnnotations;
using LuxenHotel.Models.Entities.Orders;

namespace LuxenHotel.Models.ViewModels;

public class OrderCreateViewModel
{
    public int AccommodationId { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    [StringLength(100, ErrorMessage = "Customer name cannot exceed 100 characters")]
    public string CustomerName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string CustomerEmail { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    public string CustomerPhone { get; set; }

    [Required(ErrorMessage = "Check-in date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Check-in Date")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Check-out Date")]
    public DateTime CheckOutDate { get; set; }

    [Required(ErrorMessage = "Number of guests is required")]
    [Range(1, 50, ErrorMessage = "Number of guests must be between 1 and 50")]
    [Display(Name = "Number of Guests")]
    public int NumberOfGuests { get; set; } = 1;

    [Display(Name = "Special Requests")]
    public string SpecialRequests { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
}

public class OrderEditViewModel : OrderCreateViewModel
{
    public int Id { get; set; }

    [Display(Name = "Order Status")]
    public OrderStatus Status { get; set; }

    [Display(Name = "Payment Status")]
    public PaymentStatus PaymentStatus { get; set; }
}