
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace LuxenHotel.Models.Entities.Booking;

public class Booking
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    [Key]
    public int Id { get; set; }

    public int? AccommodationId { get; set; }

    public Accommodation? Accommodation { get; set; }

    public int? ComboId { get; set; }

    public Combo? Combo { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ServicesJson { get; private set; }

    [NotMapped]
    public List<int> ServiceIds
    {
        get => string.IsNullOrEmpty(ServicesJson)
            ? new List<int>()
            : JsonSerializer.Deserialize<List<int>>(ServicesJson, _jsonOptions) ?? new List<int>();
        set => ServicesJson = value == null || value.Count == 0
            ? null
            : JsonSerializer.Serialize(value, _jsonOptions);
    }

    [Column(TypeName = "nvarchar(max)")]
    public string? ServiceQuantitiesJson { get; private set; }

    [NotMapped]
    public Dictionary<int, int> ServiceQuantities
    {
        get => string.IsNullOrEmpty(ServiceQuantitiesJson)
            ? new Dictionary<int, int>()
            : JsonSerializer.Deserialize<Dictionary<int, int>>(ServiceQuantitiesJson, _jsonOptions) ?? new Dictionary<int, int>();
        set => ServiceQuantitiesJson = value == null || value.Count == 0
            ? null
            : JsonSerializer.Serialize(value, _jsonOptions);
    }

    [Column(TypeName = "datetime")]
    public DateTime? CheckInDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CheckOutDate { get; set; }

    [Required]
    [Column(TypeName = "int")]
    [Range(0, 99999999.99, ErrorMessage = "Total price must be between 0 and 99,999,999.99")]
    public int TotalPrice { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed

    [Column(TypeName = "nvarchar(255)")]
    [StringLength(255, ErrorMessage = "Guest name cannot exceed 255 characters")]
    public string? GuestName { get; set; }

    [Column(TypeName = "nvarchar(255)")]
    [StringLength(255, ErrorMessage = "Guest contact cannot exceed 255 characters")]
    public string? GuestContact { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public void UpdateServices(List<int> serviceIds, Dictionary<int, int>? serviceQuantities = null)
    {
        ServiceIds = serviceIds ?? new List<int>();
        ServiceQuantities = serviceQuantities ?? new Dictionary<int, int>();
    }
}