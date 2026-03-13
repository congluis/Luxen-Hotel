namespace LuxenHotel.Models.ViewModels.Booking;

public class AccommodationDetailsViewModel
{
    public AccommodationViewModel Accommodation { get; set; }
    public List<ComboViewModel> Combos { get; set; }

    public AccommodationDetailsViewModel(AccommodationViewModel accommodation, List<ComboViewModel> combos)
    {
        Accommodation = accommodation;
        Combos = combos ?? new List<ComboViewModel>();
    }
}
