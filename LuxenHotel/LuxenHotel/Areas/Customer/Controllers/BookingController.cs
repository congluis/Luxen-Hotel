using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.ViewModels.Booking;
using LuxenHotel.Services.Booking.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LuxenHotel.Areas.Customer.Controllers;

[Area("Customer")]
public class BookingController : Controller
{

    private readonly IAccommodationService _accommodationService;
    private readonly IComboService _comboService;

    public BookingController(
        IAccommodationService accommodationService,
        IComboService comboService)
    {
        _accommodationService = accommodationService;
        _comboService = comboService;
    }

    [HttpGet]
    public async Task<IActionResult> Accommodations()
    {
        var viewModel = await _accommodationService.ListAsync();
        return View(viewModel);
    }

    [HttpGet]
    [Route("Accommodations/{id}")]
    public async Task<IActionResult> AccommodationDetails(int? id)
    {
        if (!id.HasValue)
            return NotFound();

        var accommodation = await _accommodationService.GetAsync(id);
        if (accommodation == null)
            return NotFound();

        var combos = await _comboService.GetCombosByAccommodationIdAsync(id!.Value);

        // Filter combos with Status = Published and non-empty Services
        var publishedCombos = combos.Where(
            c => c.Status == Combo.ComboStatus.Published &&
            c.Services != null &&
            c.Services.Any())
            .ToList();

        var model = new AccommodationDetailsViewModel(accommodation, publishedCombos);

        return View(model);
    }

    // Action xử lý đặt chỗ ở
    [HttpPost]
    public ActionResult BookAccommodations()
    {
        return View();
    }
}