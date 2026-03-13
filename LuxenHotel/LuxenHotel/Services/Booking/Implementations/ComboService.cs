using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.ViewModels.Booking;
using LuxenHotel.Services.Booking.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Services.Booking.Implementations;

public class ComboService : IComboService
{
    private readonly ApplicationDbContext _context;

    public ComboService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ComboViewModel>> ListAsync()
    {
        // Fetch combos with their accommodations and services
        var combos = await _context.Combos
            .AsNoTracking()
            .Include(c => c.Accommodation)
            .Include(c => c.ComboServices)
            .ToListAsync();

        // Map to view models
        var result = combos.Select(combo => new ComboViewModel
        {
            Id = combo.Id,
            Name = combo.Name,
            Price = combo.Price,
            Description = combo.Description,
            AccommodationId = combo.AccommodationId,
            AccommodationName = combo.Accommodation.Name,
            Status = combo.Status,
            CreatedAt = combo.CreatedAt,
            Services = combo.ComboServices.Select(service => new ServiceViewModel
            {
                Id = service.Id,
                Name = service.Name
            }).ToList(),
            SelectedServiceIds = combo.ComboServices.Select(service => service.Id).ToList()
        }).ToList();

        return result;
    }

    public async Task<List<ComboViewModel>> GetCombosByAccommodationIdAsync(int accommodationId)
    {
        return await _context.Combos
            .Where(c => c.AccommodationId == accommodationId)
            .Include(c => c.Accommodation)
            .Include(c => c.ComboServices)
            .Select(c => new ComboViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Price = c.Price,
                Description = c.Description,
                AccommodationId = c.AccommodationId,
                AccommodationName = c.Accommodation.Name,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                Services = c.ComboServices.Select(cs => new ServiceViewModel
                {
                    Id = cs.Id,
                    Name = cs.Name,
                    Price = cs.Price,
                    Description = cs.Description
                }).ToList(),
                SelectedServiceIds = c.ComboServices.Select(cs => cs.Id).ToList()
            })
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<ComboViewModel?> GetComboByIdAsync(int comboId)
    {
        try
        {
            var combo = await _context.Combos
                .AsNoTracking()
                .Where(c => c.Id == comboId)
                .Include(c => c.Accommodation)
                .Include(c => c.ComboServices)
                .Select(c => new ComboViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Price = c.Price,
                    Description = c.Description,
                    AccommodationId = c.AccommodationId,
                    AccommodationName = c.Accommodation.Name,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    Services = c.ComboServices.Select(cs => new ServiceViewModel
                    {
                        Id = cs.Id,
                        Name = cs.Name,
                        Price = cs.Price,
                        Description = cs.Description
                    }).ToList(),
                    SelectedServiceIds = c.ComboServices.Select(cs => cs.Id).ToList()
                })
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            return combo;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while retrieving the combo.", ex);
        }
    }

    public async Task<Combo> CreateComboAsync(Combo combo, List<int> selectedServiceIds)
    {
        combo.CreatedAt = DateTime.UtcNow;

        // First, save the combo without services to get an ID
        await _context.Combos.AddAsync(combo);
        await _context.SaveChangesAsync();

        // Now handle the many-to-many relationship
        if (selectedServiceIds != null && selectedServiceIds.Any())
        {
            // Get the actual service entities from the database
            var services = await _context.Services
                .Where(s => selectedServiceIds.Contains(s.Id))
                .ToListAsync();

            // Clear any existing relationships
            combo.ComboServices = new List<Service>();

            // Add each service to the combo's ComboServices collection
            foreach (var service in services)
            {
                combo.ComboServices.Add(service);
            }

            // Save the changes
            await _context.SaveChangesAsync();
        }

        return combo;
    }

    public async Task UpdateComboAsync(Combo combo, List<int> selectedServiceIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Get the existing combo from the database
            var existingCombo = await _context.Combos
                .Include(c => c.ComboServices)
                .FirstOrDefaultAsync(c => c.Id == combo.Id);

            if (existingCombo == null)
            {
                throw new Exception("Combo not found.");
            }

            // Update the combo's properties
            existingCombo.Name = combo.Name;
            existingCombo.Price = combo.Price;
            existingCombo.Description = combo.Description;
            existingCombo.AccommodationId = combo.AccommodationId;
            existingCombo.Status = combo.Status;
            existingCombo.UpdatedAt = DateTime.UtcNow;

            // Save changes to the combo
            await _context.SaveChangesAsync();

            // Get the actual service entities from the database
            var services = await _context.Services
                .Where(s => selectedServiceIds.Contains(s.Id))
                .ToListAsync();

            // Clear existing relationships
            existingCombo.ComboServices.Clear();

            // Add each selected service to the combo
            foreach (var service in services)
            {
                existingCombo.ComboServices.Add(service);
            }

            // Save the updated relationships
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteComboAsync(int comboId)
    {
        var combo = await _context.Combos.FindAsync(comboId);
        if (combo == null)
        {
            return false;
        }

        _context.Combos.Remove(combo);
        await _context.SaveChangesAsync();
        return true;
    }
}