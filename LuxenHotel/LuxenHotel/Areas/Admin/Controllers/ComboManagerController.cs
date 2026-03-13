using LuxenHotel.Models.Entities.Booking;
using LuxenHotel.Models.ViewModels.Booking;
using LuxenHotel.Services.Booking.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LuxenHotel.Areas.Admin.Controllers
{
    [Route("admin/combos")]
    public class ComboManagerController : AdminBaseController
    {
        private readonly IComboService _comboService;
        private readonly IAccommodationService _accommodationService;

        public ComboManagerController(
            ILogger<AdminBaseController> logger,
            IComboService comboService,
            IAccommodationService accommodationService) : base(logger)
        {
            _comboService = comboService
                ?? throw new ArgumentNullException(nameof(comboService));
            _accommodationService = accommodationService
                ?? throw new ArgumentNullException(nameof(accommodationService));
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var combos = await _comboService.ListAsync();
            var accommodations = await _accommodationService.GetDropdownListAsync();

            var accommodationServices = new Dictionary<int, List<ServiceViewModel>>();
            foreach (var accommodation in accommodations)
            {
                var services = await _accommodationService.GetServicesForAccommodationAsync(accommodation.Id);
                accommodationServices.Add(accommodation.Id, services);
            }

            var viewModel = new ComboListViewModel
            {
                Combos = combos,
                Accommodations = accommodations,
                AccommodationServices = accommodationServices
            };

            SetPageTitle("Combos Management");
            LogInfo("Viewed list of combos");
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ComboViewModel model)
        {
            if (!ModelState.IsValid)
            {
                HandleInvalidModelState();
                return RedirectToAction(nameof(Index), model);
            }

            try
            {
                var accommodation = await _accommodationService.GetAsync(model.AccommodationId);
                if (accommodation == null)
                {
                    TempData["ErrorMessage"] = "The selected accommodation does not exist.";
                    return RedirectToAction(nameof(Index));
                }

                var combo = new Combo
                {
                    Name = model.Name,
                    Price = model.Price,
                    Description = model.Description,
                    AccommodationId = model.AccommodationId,
                    Status = model.Status
                };

                await _comboService.CreateComboAsync(combo, model.SelectedServiceIds);

                TempData["SuccessMessage"] = "Combo created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating combo: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                AddNotification("Invalid combo ID.", NotificationType.Error);
                LogInfo("Attempted to edit combo with invalid ID");
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var combo = await _comboService.GetComboByIdAsync(id);
                if (combo == null)
                {
                    AddNotification("Combo not found.", NotificationType.Error);
                    LogInfo($"Combo with ID {id} not found");
                    return RedirectToAction(nameof(Index));
                }

                var accommodations = await _accommodationService.GetDropdownListAsync();
                var accommodationServices = await _accommodationService.GetServicesForAccommodationAsync(combo.AccommodationId);

                ViewBag.Accommodations = accommodations;
                ViewBag.AccommodationServices = accommodationServices;
                SetPageTitle($"Edit Combo - {combo.Name}");
                LogInfo($"Loaded edit page for combo ID {id}");

                return View(combo);
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while loading the combo.", ex);
            }
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ComboViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                AddNotification("Invalid combo ID.", NotificationType.Error);
                LogInfo("Mismatch between route ID and view model ID.");
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                HandleInvalidModelState();

                // Reload data for the form
                viewModel.Services = await _accommodationService.GetServicesForAccommodationAsync(viewModel.AccommodationId);
                ViewBag.Accommodations = await _accommodationService.GetDropdownListAsync();
                ViewBag.AccommodationServices = viewModel.Services;
                LogInfo($"Invalid model state for combo ID {id} edit");
                return View(viewModel);
            }

            try
            {
                var combo = new Combo
                {
                    Id = viewModel.Id,
                    Name = viewModel.Name,
                    Price = viewModel.Price,
                    Description = viewModel.Description,
                    AccommodationId = viewModel.AccommodationId,
                    Status = viewModel.Status,
                    UpdatedAt = DateTime.UtcNow
                };

                await _comboService.UpdateComboAsync(combo, viewModel.SelectedServiceIds ?? new List<int>());

                AddNotification("Combo updated successfully", NotificationType.Success);
                LogInfo($"Combo ID {id} updated successfully");
                return RedirectToAction(nameof(Edit), new { id });
            }
            catch (Exception ex)
            {
                viewModel.Services = await _accommodationService.GetServicesForAccommodationAsync(viewModel.AccommodationId);
                ViewBag.Accommodations = await _accommodationService.GetDropdownListAsync();
                ViewBag.AccommodationServices = viewModel.Services;

                return HandleError("An error occurred while updating the combo.", ex);
            }
        }

        // Helper method to prepare the view data
        private async Task PrepareEditViewData(ComboViewModel model)
        {
            var accommodations = await _accommodationService.GetDropdownListAsync();
            var accommodationServices = await _accommodationService.GetServicesForAccommodationAsync(model.AccommodationId);

            ViewBag.Accommodations = accommodations;
            ViewBag.AccommodationServices = new Dictionary<int, IEnumerable<ServiceViewModel>>
            {
                { model.AccommodationId, accommodationServices }
            };
        }
    }
}