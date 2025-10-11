using Microsoft.AspNetCore.Mvc;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class RestaurantController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
