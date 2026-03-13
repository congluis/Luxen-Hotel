using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LuxenHotel.Models.Entities.Booking;

namespace LuxenHotel.Models.ViewModels.Booking
{
    public class AccommodationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0, 50000000, ErrorMessage = "Price must be between 0 and 50,000,000")]
        public int Price { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Maximum occupancy is required")]
        [Range(1, 50, ErrorMessage = "Max occupancy must be between 1 and 50")]
        public int MaxOccupancy { get; set; }

        [Required(ErrorMessage = "Area is required")]
        [Range(0, 10000, ErrorMessage = "Area must be between 0 and 10,000 m²")]
        public decimal Area { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public Accommodation.AccommodationStatus Status { get; set; }

        // File uploads
        public List<IFormFile>? MediaFiles { get; set; }
        public IFormFile? ThumbnailFile { get; set; }

        // Existing media display
        public List<string> Media { get; set; } = new List<string>();
        public string? Thumbnail { get; set; }

        // Media deletion
        public List<string> MediaToDelete { get; set; } = new List<string>();
        public bool DeleteThumbnail { get; set; }

        // Services
        public List<ServiceViewModel> Services { get; set; } = new List<ServiceViewModel>();
        public List<int> ServicesToDelete { get; set; } = new List<int>();

        // Combos
        public List<ComboViewModel> Combos { get; set; } = new List<ComboViewModel>();
        public List<int> CombosToDelete { get; set; } = new List<int>();

        public DateTime? CreatedAt { get; set; }

        public enum AccommodationStatus
        {
            Published,
            Unpublished,
            MaintenanceMode,
            FullyBooked
        }
    }
}
