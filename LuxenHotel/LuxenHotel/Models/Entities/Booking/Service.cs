
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuxenHotel.Models.Entities.Booking;

public class Service
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    public required string Name { get; set; }

    [Required]
    [Column(TypeName = "int")]
    [Range(0, 50000000, ErrorMessage = "Price must be between 0 and 50,000,000")]
    public int Price { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Required]
    public int AccommodationId { get; set; }
    public Accommodation? Accommodation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<Combo> ComboServices { get; set; } = new();

}