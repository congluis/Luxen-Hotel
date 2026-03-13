using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.ViewModels.Booking;

namespace LuxenHotel.Services.Booking.Interfaces
{
    public interface IComboService
    {
        Task<List<ComboViewModel>> ListAsync();
        Task<List<ComboViewModel>> GetCombosByAccommodationIdAsync(int accommodationId);
        Task<ComboViewModel> GetComboByIdAsync(int comboId);
        Task<Combo> CreateComboAsync(Combo combo, List<int> selectedServiceIds);
        Task UpdateComboAsync(Combo combo, List<int> selectedServiceIds);
        Task<bool> DeleteComboAsync(int comboId);
    }
}