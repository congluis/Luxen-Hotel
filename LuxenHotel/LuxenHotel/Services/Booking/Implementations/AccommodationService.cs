using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.ViewModels.Booking;
using LuxenHotel.Services.Booking.Interfaces;
using LuxenHotel.Helpers;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Services.Booking.Implementations
{
    public class AccommodationService : IAccommodationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccommodationService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public async Task<List<AccommodationDropdownItem>> GetDropdownListAsync()
        {
            return await _context.Accommodations
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .Select(a => new AccommodationDropdownItem
                {
                    Id = a.Id,
                    Name = a.Name
                })
                .ToListAsync();
        }

        public async Task<List<AccommodationViewModel>> ListAsync()
        {
            var accommodations = await _context.Accommodations
                .AsNoTracking()
                .Include(a => a.Services)
                .ToListAsync();

            return accommodations.Select(ToViewModel).ToList();
        }

        public async Task<AccommodationViewModel?> GetAsync(int? id)
        {
            if (!id.HasValue)
                return null;

            var accommodation = await _context.Accommodations
                .AsNoTracking()
                .Include(a => a.Services)
                .Include(a => a.Combos)
                    .ThenInclude(c => c.ComboServices)
                .FirstOrDefaultAsync(m => m.Id == id.Value);

            return accommodation == null ? null : ToViewModel(accommodation);
        }

        public async Task<List<ServiceViewModel>> GetServicesForAccommodationAsync(int accommodationId)
        {
            var accommodation = await _context.Accommodations
                .AsNoTracking()
                .Include(a => a.Services)
                .FirstOrDefaultAsync(a => a.Id == accommodationId);

            if (accommodation == null || accommodation.Services == null)
            {
                return new List<ServiceViewModel>();
            }

            return accommodation.Services.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                Description = s.Description
            }).ToList();
        }

        public async Task CreateAsync(AccommodationViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            var accommodation = new Accommodation
            {
                Name = viewModel.Name,
                Price = viewModel.Price,
                Description = viewModel.Description,
                MaxOccupancy = viewModel.MaxOccupancy,
                Area = viewModel.Area,
                Status = viewModel.Status,
                CreatedAt = DateTime.UtcNow
            };

            await UpdateMediaAsync(accommodation, viewModel);
            UpdateServices(accommodation, viewModel.Services);

            _context.Accommodations.Add(accommodation);
            await _context.SaveChangesAsync();
        }

        public async Task EditAsync(int id, AccommodationViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            var accommodation = await _context.Accommodations
                .Include(a => a.Services)
                .Include(a => a.Combos)
                    .ThenInclude(c => c.ComboServices)
                .FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException($"Accommodation with ID {id} not found.");

            // Update basic properties
            accommodation.Name = viewModel.Name;
            accommodation.Price = viewModel.Price;
            accommodation.Description = viewModel.Description;
            accommodation.MaxOccupancy = viewModel.MaxOccupancy;
            accommodation.Area = viewModel.Area;
            accommodation.Status = viewModel.Status;
            accommodation.UpdatedAt = DateTime.UtcNow;

            await UpdateMediaAsync(accommodation, viewModel);

            // Extract services and combos to delete
            var servicesToDelete = ExtractServicesToDelete(viewModel);
            var combosToDelete = ExtractCombosToDelete(viewModel);

            // Update services and combos
            UpdateServices(accommodation, viewModel.Services, servicesToDelete);
            UpdateCombos(accommodation, viewModel.Combos, combosToDelete);

            await _context.SaveChangesAsync();
        }

        private int[] ExtractServicesToDelete(AccommodationViewModel viewModel)
        {
            // Check if the ServicesToDelete property exists in the viewModel
            if (viewModel.ServicesToDelete != null && viewModel.ServicesToDelete.Any())
            {
                return viewModel.ServicesToDelete.ToArray();
            }
            return Array.Empty<int>();
        }

        private async Task UpdateMediaAsync(Accommodation accommodation, AccommodationViewModel viewModel)
        {
            // Handle media files to delete
            if (viewModel.MediaToDelete?.Any() == true)
            {
                var filesToDelete = new List<string>();
                foreach (var mediaPath in viewModel.MediaToDelete)
                {
                    if (accommodation.Media.Contains(mediaPath))
                    {
                        filesToDelete.Add(mediaPath);
                        accommodation.Media.Remove(mediaPath);
                    }
                }

                // Delete the files from the file system
                FileStorageService.DeleteFiles(filesToDelete, _environment);
            }

            // Handle media uploads
            if (viewModel.MediaFiles?.Any() == true)
            {
                var mediaPaths = await FileStorageService.UploadFilesAsync(viewModel.MediaFiles, _environment);
                accommodation.UpdateMedia(mediaPaths);
            }

            // Handle thumbnail deletion
            if (viewModel.DeleteThumbnail && !string.IsNullOrEmpty(accommodation.Thumbnail))
            {
                FileStorageService.DeleteFile(accommodation.Thumbnail, _environment);
                accommodation.Thumbnail = null;
            }

            // Handle thumbnail upload
            if (viewModel.ThumbnailFile?.Length > 0)
            {
                // If we have an existing thumbnail, delete it first
                if (!string.IsNullOrEmpty(accommodation.Thumbnail))
                {
                    FileStorageService.DeleteFile(accommodation.Thumbnail, _environment);
                }

                var thumbnailPath = await FileStorageService.UploadSingleFileAsync(viewModel.ThumbnailFile, _environment);
                if (thumbnailPath != null)
                {
                    accommodation.Thumbnail = thumbnailPath;
                }
            }
        }

        private void UpdateServices(Accommodation accommodation, IList<ServiceViewModel>? services, int[]? servicesToDelete = null)
        {
            // Handle deletions if any services are marked for deletion
            if (servicesToDelete != null && servicesToDelete.Length > 0)
            {
                // Find and remove services marked for deletion
                foreach (var serviceId in servicesToDelete)
                {
                    var serviceToRemove = accommodation.Services.FirstOrDefault(s => s.Id == serviceId);
                    if (serviceToRemove != null)
                    {
                        accommodation.Services.Remove(serviceToRemove);
                    }
                }
            }

            // Update existing services and add new ones
            if (services?.Any() == true)
            {
                foreach (var serviceViewModel in services)
                {
                    // Skip empty services
                    if (string.IsNullOrEmpty(serviceViewModel.Name))
                        continue;

                    // Case 1: Existing service (has ID)
                    if (serviceViewModel.Id > 0)
                    {
                        // Find and update the existing service
                        var existingService = accommodation.Services.FirstOrDefault(s => s.Id == serviceViewModel.Id);
                        if (existingService != null)
                        {
                            existingService.Name = serviceViewModel.Name;
                            existingService.Price = serviceViewModel.Price;
                            existingService.Description = serviceViewModel.Description;
                            existingService.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    // Case 2: New service (no ID)
                    else
                    {
                        accommodation.Services.Add(new Service
                        {
                            Name = serviceViewModel.Name,
                            Price = serviceViewModel.Price,
                            Description = serviceViewModel.Description,
                            CreatedAt = DateTime.UtcNow,
                            Accommodation = accommodation
                        });
                    }
                }
            }
        }

        private int[] ExtractCombosToDelete(AccommodationViewModel viewModel)
        {
            // Check if the CombosToDelete property exists in the viewModel
            if (viewModel.CombosToDelete != null && viewModel.CombosToDelete.Any())
            {
                return viewModel.CombosToDelete.ToArray();
            }
            return Array.Empty<int>();
        }

        private void UpdateCombos(Accommodation accommodation, IList<ComboViewModel>? combos, int[]? combosToDelete = null)
        {
            // Handle deletions if any combos are marked for deletion
            if (combosToDelete != null && combosToDelete.Length > 0)
            {
                // Find and remove combos marked for deletion
                foreach (var comboId in combosToDelete)
                {
                    var comboToRemove = accommodation.Combos.FirstOrDefault(c => c.Id == comboId);
                    if (comboToRemove != null)
                    {
                        accommodation.Combos.Remove(comboToRemove);
                    }
                }
            }

            // Update existing combos and add new ones
            if (combos?.Any() == true)
            {
                foreach (var comboViewModel in combos)
                {
                    // Skip empty combos
                    if (string.IsNullOrEmpty(comboViewModel.Name))
                        continue;

                    // Case 1: Existing combo (has ID)
                    if (comboViewModel.Id > 0)
                    {
                        // Find and update the existing combo
                        var existingCombo = accommodation.Combos.FirstOrDefault(c => c.Id == comboViewModel.Id);
                        if (existingCombo != null)
                        {
                            existingCombo.Name = comboViewModel.Name;
                            existingCombo.Price = comboViewModel.Price;
                            existingCombo.Description = comboViewModel.Description;
                            existingCombo.Status = comboViewModel.Status;
                            existingCombo.UpdatedAt = DateTime.UtcNow;

                            // Update combo services
                            UpdateComboServices(existingCombo, comboViewModel.SelectedServiceIds, accommodation);
                        }
                    }
                    // Case 2: New combo (no ID)
                    else
                    {
                        var newCombo = new Combo
                        {
                            Name = comboViewModel.Name,
                            Price = comboViewModel.Price,
                            Description = comboViewModel.Description,
                            Status = comboViewModel.Status,
                            CreatedAt = DateTime.UtcNow,
                            Accommodation = accommodation
                        };

                        // Add services to the new combo
                        UpdateComboServices(newCombo, comboViewModel.SelectedServiceIds, accommodation);

                        accommodation.Combos.Add(newCombo);
                    }
                }
            }
        }

        private void UpdateComboServices(Combo combo, List<int> selectedServiceIds, Accommodation accommodation)
        {
            // Clear existing services from the combo
            combo.ComboServices.Clear();

            // Add selected services
            if (selectedServiceIds != null && selectedServiceIds.Any())
            {
                foreach (var serviceId in selectedServiceIds)
                {
                    // Find the service in the accommodation's services or from the database
                    var service = accommodation.Services.FirstOrDefault(s => s.Id == serviceId);

                    if (service != null)
                    {
                        combo.ComboServices.Add(service);
                    }
                }
            }
        }

        private AccommodationViewModel ToViewModel(Accommodation accommodation) => new()
        {
            Id = accommodation.Id,
            Name = accommodation.Name,
            Price = accommodation.Price,
            Description = accommodation.Description,
            MaxOccupancy = accommodation.MaxOccupancy,
            Area = accommodation.Area,
            Status = accommodation.Status,
            Media = accommodation.Media?.ToList() ?? new List<string>(),
            Thumbnail = accommodation.Thumbnail,
            CreatedAt = accommodation.CreatedAt,
            Services = accommodation.Services?.Select(s => new ServiceViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                Description = s.Description
            }).ToList() ?? new List<ServiceViewModel>(),
            Combos = accommodation.Combos?.Select(c => new ComboViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Price = c.Price,
                Description = c.Description,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                SelectedServiceIds = c.ComboServices?.Select(s => s.Id).ToList() ?? new List<int>()
            }).ToList() ?? new List<ComboViewModel>()
        };

        public async Task DeleteAsync(int id)
        {
            var accommodation = await _context.Accommodations
                .Include(a => a.Services)
                .FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException($"Accommodation with ID {id} not found.");

            // Delete associated media files
            if (!string.IsNullOrEmpty(accommodation.Thumbnail))
            {
                var thumbnailPath = Path.Combine(_environment.WebRootPath, accommodation.Thumbnail.TrimStart('/'));
                if (File.Exists(thumbnailPath))
                    File.Delete(thumbnailPath);
            }

            if (accommodation.Media?.Any() == true)
            {
                foreach (var mediaPath in accommodation.Media)
                {
                    var fullPath = Path.Combine(_environment.WebRootPath, mediaPath.TrimStart('/'));
                    if (File.Exists(fullPath))
                        File.Delete(fullPath);
                }
            }

            _context.Accommodations.Remove(accommodation);
            await _context.SaveChangesAsync();
        }
    }
}