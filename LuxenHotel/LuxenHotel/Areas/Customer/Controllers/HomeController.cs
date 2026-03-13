using Microsoft.AspNetCore.Mvc;

namespace LuxenHotel.Areas.Customer.Controllers
{
    [Area("Customer")]
    // [Authorize(Roles = "Customer")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}