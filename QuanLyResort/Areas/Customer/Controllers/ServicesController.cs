using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ServicesController : Controller
    {
        private readonly ResortDbContext _context;

        public ServicesController(ResortDbContext context)
        {
            _context = context;
        }

        // GET: Customer/Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .Include(s => s.ServiceCategory)
                .Where(s => s.IsActive == true)
                .ToListAsync();
            return View(services);
        }

        // GET: Customer/Services/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.ServiceCategory)
                .FirstOrDefaultAsync(m => m.ServiceId == id);
            if (service == null)
            {
                return NotFound();
            }

            // Lấy các dịch vụ liên quan
            var relatedServices = await _context.Services
                .Include(s => s.ServiceCategory)
                .Where(s => s.CategoryId == service.CategoryId && s.ServiceId != service.ServiceId && s.IsActive == true)
                .Take(3)
                .ToListAsync();

            ViewBag.RelatedServices = relatedServices;

            return View(service);
        }

        // GET: Customer/Services/ByCategory/5
        public async Task<IActionResult> ByCategory(int? categoryId)
        {
            var services = _context.Services
                .Include(s => s.ServiceCategory)
                .Where(s => s.IsActive == true)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                services = services.Where(s => s.CategoryId == categoryId.Value);
            }

            var categories = await _context.ServiceCategories.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;

            return View(await services.ToListAsync());
        }
    }
}
