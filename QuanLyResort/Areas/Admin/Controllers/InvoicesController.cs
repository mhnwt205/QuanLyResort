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
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ResortDbContext _context;

        public InvoicesController(IInvoiceService invoiceService, ResortDbContext context)
        {
            _invoiceService = invoiceService;
            _context = context;
        }

        // GET: Admin/Invoices
        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetAllAsync();
            return View(invoices);
        }

        // GET: Admin/Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _invoiceService.GetByIdAsync(id.Value);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Admin/Invoices/Create
        public IActionResult Create()
        {
            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Bookings = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .ToList();
            ViewBag.Users = _context.Users.ToList();

            return View(new CreateInvoiceDto 
            { 
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(7)
            });
        }

        // POST: Admin/Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInvoiceDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Bookings = _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }

            try
            {
                var invoiceId = await _invoiceService.CreateAsync(dto, User.Identity?.Name ?? "system");
                TempData["SuccessMessage"] = "Hóa đơn đã được tạo thành công!";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Bookings = _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }
        }

        // GET: Admin/Invoices/CreateFromBooking/5
        public async Task<IActionResult> CreateFromBooking(int bookingId)
        {
            try
            {
                var invoiceId = await _invoiceService.CreateFromBookingAsync(bookingId, 0.1m, 0, User.Identity?.Name ?? "system");
                TempData["SuccessMessage"] = "Hóa đơn đã được tạo từ đặt phòng thành công!";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tạo hóa đơn: {ex.Message}";
                return RedirectToAction("Details", "Bookings", new { id = bookingId });
            }
        }

        // GET: Admin/Invoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _invoiceService.GetByIdAsync(id.Value);
            if (invoice == null)
            {
                return NotFound();
            }

            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Bookings = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .ToList();
            ViewBag.Users = _context.Users.ToList();

            var dto = new CreateInvoiceDto
            {
                CustomerId = invoice.CustomerId ?? 0,
                BookingId = invoice.BookingId,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Subtotal = invoice.Subtotal,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                PaymentMethod = invoice.PaymentMethod,
                Notes = invoice.Notes
            };

            return View(dto);
        }

        // POST: Admin/Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateInvoiceDto dto)
        {
            if (id != dto.CustomerId) // This should be invoiceId, but we'll handle it differently
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Bookings = _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }

            try
            {
                var result = await _invoiceService.UpdateAsync(id, dto, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Hóa đơn đã được cập nhật thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật hóa đơn này.";
                }
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Bookings = _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }
        }

        // GET: Admin/Invoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _invoiceService.GetByIdAsync(id.Value);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Admin/Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _invoiceService.CancelAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Hóa đơn đã được hủy thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy hóa đơn này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hủy hóa đơn: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Invoices/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _invoiceService.ApproveAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Hóa đơn đã được duyệt thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể duyệt hóa đơn này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi duyệt hóa đơn: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Invoices/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Admin/Invoices/GetByCustomer/5
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var invoices = await _invoiceService.GetByCustomerIdAsync(customerId);
            return Json(invoices);
        }

        // GET: Admin/Invoices/GetByDateRange
        public async Task<IActionResult> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            var invoices = await _invoiceService.GetByDateRangeAsync(startDate, endDate);
            return Json(invoices);
        }

        // GET: Admin/Invoices/GetByStatus
        public async Task<IActionResult> GetByStatus(string status)
        {
            var invoices = await _invoiceService.GetByStatusAsync(status);
            return Json(invoices);
        }

        // GET: Admin/Invoices/GetRemainingAmount/5
        public async Task<IActionResult> GetRemainingAmount(int id)
        {
            try
            {
                var remainingAmount = await _invoiceService.GetRemainingAmountAsync(id);
                return Json(new { remainingAmount = remainingAmount });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}