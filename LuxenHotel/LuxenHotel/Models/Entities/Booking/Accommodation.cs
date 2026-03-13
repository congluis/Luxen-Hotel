using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Models.Entities.Booking;

public class Accommodation
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

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

    [NotMapped]
    public List<string> Media
    {
        get => string.IsNullOrEmpty(MediaJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(MediaJson, _jsonOptions) ?? new List<string>();
        set => MediaJson = value == null || value.Count == 0
            ? null
            : JsonSerializer.Serialize(value, _jsonOptions);
    }

    [Column("Media", TypeName = "nvarchar(max)")]
    public string? MediaJson { get; private set; }

    [NotMapped]
    public string? Thumbnail
    {
        get => string.IsNullOrEmpty(ThumbnailJson)
            ? null
            : JsonSerializer.Deserialize<string>(ThumbnailJson, _jsonOptions);
        set => ThumbnailJson = string.IsNullOrEmpty(value)
            ? null
            : JsonSerializer.Serialize(value, _jsonOptions);
    }

    [Column("Thumbnail", TypeName = "nvarchar(max)")]
    public string? ThumbnailJson { get; private set; }

    [Required]
    [Column(TypeName = "int")]
    public AccommodationStatus Status { get; set; } = new();

    [Required]
    [Range(1, 50, ErrorMessage = "Max occupancy must be between 1 and 50")]
    public int MaxOccupancy { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Required]
    [Range(0, 10000, ErrorMessage = "Area must be between 0 and 10,000 m²")]
    public decimal Area { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public List<Service> Services { get; set; } = new();
    public List<Combo> Combos { get; set; } = new();

    public void UpdateMedia(List<string> media)
    {
        Media = media ?? new List<string>();
    }
    public enum AccommodationStatus
    {
        Published,
        Unpublished,
        MaintenanceMode,
        FullyBooked
    }
}
