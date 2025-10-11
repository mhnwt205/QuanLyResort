using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ServicesController : Controller
    {
        private readonly ResortDbContext _context;

        public ServicesController(ResortDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Services
        public async Task<IActionResult> Index()
        {
            var services = await _context.Services
                .Include(s => s.Category)
                .OrderBy(s => s.ServiceName)
                .ToListAsync();
            return View(services);
        }

        // GET: Admin/Services/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.Category)
                .FirstOrDefaultAsync(m => m.ServiceId == id);
            if (service == null)
            {
                return NotFound();
            }

            // Lấy lịch sử đặt dịch vụ
            var serviceBookings = await _context.ServiceBookings
                .Include(sb => sb.Customer)
                .Where(sb => sb.ServiceId == id)
                .OrderByDescending(sb => sb.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.ServiceBookings = serviceBookings;

            return View(service);
        }

        // GET: Admin/Services/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryId"] = await _context.ServiceCategories.ToListAsync();
            return View();
        }

        // POST: Admin/Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceId,ServiceCode,ServiceName,CategoryId,Description,UnitPrice,Unit,IsActive")] Service service)
        {
            if (ModelState.IsValid)
            {
                service.CreatedAt = DateTime.Now;
                service.UpdatedAt = DateTime.Now;
                _context.Add(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm dịch vụ thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = await _context.ServiceCategories.ToListAsync();
            return View(service);
        }

        // GET: Admin/Services/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = await _context.ServiceCategories.ToListAsync();
            return View(service);
        }

        // POST: Admin/Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceId,ServiceCode,ServiceName,CategoryId,Description,UnitPrice,Unit,IsActive")] Service service)
        {
            if (id != service.ServiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    service.UpdatedAt = DateTime.Now;
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật dịch vụ thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.ServiceId))
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
            ViewData["CategoryId"] = await _context.ServiceCategories.ToListAsync();
            return View(service);
        }

        // GET: Admin/Services/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var service = await _context.Services
                .Include(s => s.Category)
                .FirstOrDefaultAsync(m => m.ServiceId == id);
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // POST: Admin/Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa dịch vụ thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Services/ServiceBookings
        public async Task<IActionResult> ServiceBookings()
        {
            var serviceBookings = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Include(sb => sb.Customer)
                .Include(sb => sb.CreatedByNavigation)
                .OrderByDescending(sb => sb.CreatedAt)
                .ToListAsync();
            return View(serviceBookings);
        }

        // GET: Admin/Services/ServiceBookings/Details/5
        public async Task<IActionResult> ServiceBookingDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceBooking = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Include(sb => sb.Customer)
                .Include(sb => sb.CreatedByNavigation)
                .FirstOrDefaultAsync(m => m.ServiceBookingId == id);

            if (serviceBooking == null)
            {
                return NotFound();
            }

            return View(serviceBooking);
        }

        // POST: Admin/Services/ServiceBookings/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveServiceBooking(int id)
        {
            var serviceBooking = await _context.ServiceBookings.FindAsync(id);
            if (serviceBooking != null)
            {
                serviceBooking.Status = "approved";
                serviceBooking.UpdatedAt = DateTime.Now;
                _context.Update(serviceBooking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Duyệt đặt dịch vụ thành công!";
            }
            return RedirectToAction(nameof(ServiceBookings));
        }

        // POST: Admin/Services/ServiceBookings/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteServiceBooking(int id)
        {
            var serviceBooking = await _context.ServiceBookings.FindAsync(id);
            if (serviceBooking != null)
            {
                serviceBooking.Status = "completed";
                serviceBooking.UpdatedAt = DateTime.Now;
                _context.Update(serviceBooking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hoàn thành dịch vụ thành công!";
            }
            return RedirectToAction(nameof(ServiceBookings));
        }

        // POST: Admin/Services/ServiceBookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelServiceBooking(int id)
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
            return RedirectToAction(nameof(ServiceBookings));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.ServiceId == id);
        }
    }
}
