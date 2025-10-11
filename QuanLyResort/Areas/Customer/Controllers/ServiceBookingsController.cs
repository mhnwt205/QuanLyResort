using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ServiceBookingsController : Controller
    {
        private readonly ResortDbContext _context;

        public ServiceBookingsController(ResortDbContext context)
        {
            _context = context;
        }

        // GET: Customer/ServiceBookings
        public async Task<IActionResult> Index()
        {
            var serviceBookings = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Include(sb => sb.Customer)
                .OrderByDescending(sb => sb.CreatedAt)
                .ToListAsync();
            return View(serviceBookings);
        }

        // GET: Customer/ServiceBookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceBooking = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Include(sb => sb.Customer)
                .FirstOrDefaultAsync(m => m.ServiceBookingId == id);

            if (serviceBooking == null)
            {
                return NotFound();
            }

            return View(serviceBooking);
        }

        // GET: Customer/ServiceBookings/Create
        public async Task<IActionResult> Create(int? serviceId, int? customerId)
        {
            var services = await _context.Services
                .Include(s => s.Category)
                .Where(s => s.IsActive == true)
                .ToListAsync();
            var customers = await _context.Customers.ToListAsync();

            ViewBag.ServiceId = services;
            ViewBag.CustomerId = customers;
            ViewBag.SelectedServiceId = serviceId;
            ViewBag.SelectedCustomerId = customerId;

            if (serviceId.HasValue)
            {
                var service = await _context.Services
                    .Include(s => s.Category)
                    .FirstOrDefaultAsync(s => s.ServiceId == serviceId.Value);
                
                if (service != null)
                {
                    ViewBag.SelectedService = service;
                }
            }

            return View();
        }

        // POST: Customer/ServiceBookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceBookingId,CustomerId,ServiceId,BookingDate,ServiceDate,Quantity,SpecialRequests")] ServiceBooking serviceBooking)
        {
            if (ModelState.IsValid)
            {
                // Lấy thông tin dịch vụ để tính giá
                var service = await _context.Services.FindAsync(serviceBooking.ServiceId);
                if (service != null)
                {
                    serviceBooking.UnitPrice = service.UnitPrice;
                    serviceBooking.TotalAmount = service.UnitPrice * (serviceBooking.Quantity ?? 1);
                }

                // Tạo mã đặt dịch vụ tự động
                serviceBooking.BookingCode = await GenerateServiceBookingCode();
                serviceBooking.CreatedAt = DateTime.Now;
                serviceBooking.UpdatedAt = DateTime.Now;
                serviceBooking.Status = "pending";

                _context.Add(serviceBooking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đặt dịch vụ thành công! Mã đặt dịch vụ: " + serviceBooking.BookingCode;
                return RedirectToAction(nameof(Index));
            }

            var services = await _context.Services
                .Include(s => s.Category)
                .Where(s => s.IsActive == true)
                .ToListAsync();
            var customers = await _context.Customers.ToListAsync();

            ViewBag.ServiceId = services;
            ViewBag.CustomerId = customers;
            return View(serviceBooking);
        }

        // GET: Customer/ServiceBookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceBooking = await _context.ServiceBookings.FindAsync(id);
            if (serviceBooking == null)
            {
                return NotFound();
            }

            var services = await _context.Services
                .Include(s => s.Category)
                .Where(s => s.IsActive == true)
                .ToListAsync();
            var customers = await _context.Customers.ToListAsync();

            ViewBag.ServiceId = services;
            ViewBag.CustomerId = customers;
            return View(serviceBooking);
        }

        // POST: Customer/ServiceBookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceBookingId,CustomerId,ServiceId,BookingDate,ServiceDate,Quantity,SpecialRequests")] ServiceBooking serviceBooking)
        {
            if (id != serviceBooking.ServiceBookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật giá nếu dịch vụ thay đổi
                    var service = await _context.Services.FindAsync(serviceBooking.ServiceId);
                    if (service != null)
                    {
                        serviceBooking.UnitPrice = service.UnitPrice;
                        serviceBooking.TotalAmount = service.UnitPrice * (serviceBooking.Quantity ?? 1);
                    }

                    serviceBooking.UpdatedAt = DateTime.Now;
                    _context.Update(serviceBooking);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật đặt dịch vụ thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceBookingExists(serviceBooking.ServiceBookingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var services = await _context.Services
                .Include(s => s.Category)
                .Where(s => s.IsActive == true)
                .ToListAsync();
            var customers = await _context.Customers.ToListAsync();

            ViewBag.ServiceId = services;
            ViewBag.CustomerId = customers;
            return View(serviceBooking);
        }

        // POST: Customer/ServiceBookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var serviceBooking = await _context.ServiceBookings.FindAsync(id);
            if (serviceBooking != null)
            {
                serviceBooking.Status = "cancelled";
                serviceBooking.UpdatedAt = DateTime.Now;
                _context.Update(serviceBooking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hủy đặt dịch vụ thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Customer/ServiceBookings/ByCategory/5
        public async Task<IActionResult> ByCategory(int? categoryId)
        {
            var services = _context.Services
                .Include(s => s.Category)
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

        // GET: Customer/ServiceBookings/GetServicePrice/5
        [HttpGet]
        public async Task<IActionResult> GetServicePrice(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dịch vụ" });
            }

            return Json(new { 
                success = true, 
                unitPrice = service.UnitPrice,
                unit = service.Unit,
                description = service.Description
            });
        }

        private bool ServiceBookingExists(int id)
        {
            return _context.ServiceBookings.Any(e => e.ServiceBookingId == id);
        }

        private async Task<string> GenerateServiceBookingCode()
        {
            var today = DateTime.Today;
            var prefix = $"SB{today:yyyyMMdd}";
            var lastServiceBooking = await _context.ServiceBookings
                .Where(sb => sb.BookingCode.StartsWith(prefix))
                .OrderByDescending(sb => sb.BookingCode)
                .FirstOrDefaultAsync();

            if (lastServiceBooking == null)
            {
                return $"{prefix}001";
            }

            var lastNumber = int.Parse(lastServiceBooking.BookingCode.Substring(prefix.Length));
            return $"{prefix}{(lastNumber + 1):D3}";
        }
    }
}
