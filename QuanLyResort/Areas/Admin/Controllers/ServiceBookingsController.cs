using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyResort.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ServiceBookingsController : Controller
    {
        private readonly IServiceBookingService _serviceBookingService;
        private readonly ResortDbContext _context;

        public ServiceBookingsController(IServiceBookingService serviceBookingService, ResortDbContext context)
        {
            _serviceBookingService = serviceBookingService;
            _context = context;
        }

        // GET: Admin/ServiceBookings
        public async Task<IActionResult> Index()
        {
            var serviceBookings = await _serviceBookingService.GetAllAsync();
            return View(serviceBookings);
        }

        // GET: Admin/ServiceBookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceBooking = await _serviceBookingService.GetByIdAsync(id.Value);
            if (serviceBooking == null)
            {
                return NotFound();
            }

            return View(serviceBooking);
        }

        // GET: Admin/ServiceBookings/Create
        public IActionResult Create()
        {
            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Services = _context.Services
                .Include(s => s.Category)
                .Where(s => s.IsActive == true)
                .ToList();

            return View(new CreateServiceBookingDto 
            { 
                BookingDate = DateTime.Today,
                ServiceDate = DateTime.Today
            });
        }

        // POST: Admin/ServiceBookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateServiceBookingDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Services = _context.Services
                    .Include(s => s.Category)
                    .Where(s => s.IsActive == true)
                    .ToList();
                return View(dto);
            }

            try
            {
                var serviceBookingId = await _serviceBookingService.CreateAsync(dto, User.Identity?.Name ?? "system");
                TempData["SuccessMessage"] = "Đặt dịch vụ đã được tạo thành công!";
                return RedirectToAction(nameof(Details), new { id = serviceBookingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Services = _context.Services
                    .Include(s => s.Category)
                    .Where(s => s.IsActive == true)
                    .ToList();
                return View(dto);
            }
        }

        // GET: Admin/ServiceBookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceBooking = await _serviceBookingService.GetByIdAsync(id.Value);
            if (serviceBooking == null)
            {
                return NotFound();
            }

            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Services = _context.Services
                .Include(s => s.Category)
                .Where(s => s.IsActive == true)
                .ToList();

            var dto = new CreateServiceBookingDto
            {
                CustomerId = serviceBooking.CustomerId ?? 0,
                ServiceId = serviceBooking.ServiceId,
                BookingDate = serviceBooking.BookingDate,
                ServiceDate = serviceBooking.ServiceDate,
                Quantity = serviceBooking.Quantity,
                SpecialRequests = serviceBooking.SpecialRequests
            };

            return View(dto);
        }

        // POST: Admin/ServiceBookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateServiceBookingDto dto)
        {
            if (id != dto.CustomerId) // This should be serviceBookingId, but we'll handle it differently
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Services = _context.Services
                    .Include(s => s.Category)
                    .Where(s => s.IsActive == true)
                    .ToList();
                return View(dto);
            }

            try
            {
                var result = await _serviceBookingService.UpdateAsync(id, dto, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Đặt dịch vụ đã được cập nhật thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật đặt dịch vụ này.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Services = _context.Services
                    .Include(s => s.Category)
                    .Where(s => s.IsActive == true)
                    .ToList();
                return View(dto);
            }
        }

        // GET: Admin/ServiceBookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceBooking = await _serviceBookingService.GetByIdAsync(id.Value);
            if (serviceBooking == null)
            {
                return NotFound();
            }

            return View(serviceBooking);
        }

        // POST: Admin/ServiceBookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _serviceBookingService.CancelAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Đặt dịch vụ đã được hủy thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy đặt dịch vụ này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hủy đặt dịch vụ: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/ServiceBookings/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _serviceBookingService.ApproveAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Đặt dịch vụ đã được duyệt thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể duyệt đặt dịch vụ này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi duyệt đặt dịch vụ: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/ServiceBookings/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                var result = await _serviceBookingService.CompleteAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Dịch vụ đã được hoàn thành thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hoàn thành dịch vụ này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hoàn thành dịch vụ: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/ServiceBookings/GetByCustomer/5
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var serviceBookings = await _serviceBookingService.GetByCustomerIdAsync(customerId);
            return Json(serviceBookings);
        }

        // GET: Admin/ServiceBookings/GetByService/5
        public async Task<IActionResult> GetByService(int serviceId)
        {
            var serviceBookings = await _serviceBookingService.GetByServiceIdAsync(serviceId);
            return Json(serviceBookings);
        }

        // GET: Admin/ServiceBookings/GetByDateRange
        public async Task<IActionResult> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            var serviceBookings = await _serviceBookingService.GetByDateRangeAsync(startDate, endDate);
            return Json(serviceBookings);
        }

        // GET: Admin/ServiceBookings/GetByStatus
        public async Task<IActionResult> GetByStatus(string status)
        {
            var serviceBookings = await _serviceBookingService.GetByStatusAsync(status);
            return Json(serviceBookings);
        }

        // GET: Admin/ServiceBookings/CalculateAmount
        [HttpGet]
        public async Task<IActionResult> CalculateAmount(int serviceId, int quantity)
        {
            try
            {
                var amount = await _serviceBookingService.CalculateTotalAmountAsync(serviceId, quantity);
                return Json(new { amount = amount });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
