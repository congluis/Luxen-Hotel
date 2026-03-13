using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.ViewModels.Booking;

namespace LuxenHotel.Services.Booking.Interfaces;

public interface IAccommodationService
{
    Task<List<AccommodationDropdownItem>> GetDropdownListAsync();
    Task<List<AccommodationViewModel>> ListAsync();
    Task<AccommodationViewModel> GetAsync(int? id);
    Task<List<ServiceViewModel>> GetServicesForAccommodationAsync(int accommodationId);
    Task CreateAsync(AccommodationViewModel viewModel);
    Task EditAsync(int id, AccommodationViewModel viewModel);
    Task DeleteAsync(int id);
}