using Microsoft.AspNetCore.Mvc;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
