using System.ComponentModel.DataAnnotations;
using LuxenHotel.Models.Entities.Booking;

namespace LuxenHotel.Models.ViewModels.Booking
{
    public class ComboViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 50000000, ErrorMessage = "Price must be between 0 and 50,000,000")]
        public int Price { get; set; }

        public string? Description { get; set; }

        public int AccommodationId { get; set; }
        public string? AccommodationName { get; set; }

        public Combo.ComboStatus Status { get; set; } = Combo.ComboStatus.Published;

        public IEnumerable<ServiceViewModel>? Services { get; set; } = new List<ServiceViewModel>();

        // Selected service IDs to be included in this combo
        public List<int> SelectedServiceIds { get; set; } = new List<int>();

        public DateTime? CreatedAt { get; set; }
    }

    public class ComboListViewModel
    {
        public List<ComboViewModel> Combos { get; set; }
        public List<AccommodationDropdownItem> Accommodations { get; set; }
        public Dictionary<int, List<ServiceViewModel>> AccommodationServices { get; set; } =
            new Dictionary<int, List<ServiceViewModel>>();
    }

    public class AccommodationDropdownItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}