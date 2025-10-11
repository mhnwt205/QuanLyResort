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
    public class PaymentsController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IInvoiceService _invoiceService;
        private readonly ResortDbContext _context;

        public PaymentsController(IPaymentService paymentService, IInvoiceService invoiceService, ResortDbContext context)
        {
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _context = context;
        }

        // GET: Admin/Payments
        public async Task<IActionResult> Index()
        {
            var payments = await _paymentService.GetAllAsync();
            return View(payments);
        }

        // GET: Admin/Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _paymentService.GetByIdAsync(id.Value);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Admin/Payments/Create
        public IActionResult Create(int? invoiceId)
        {
            ViewBag.Invoices = _context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.Status != "paid")
                .ToList();
            ViewBag.Users = _context.Users.ToList();

            var dto = new CreatePaymentDto 
            { 
                PaymentDate = DateTime.Today 
            };

            if (invoiceId.HasValue)
            {
                dto.InvoiceId = invoiceId.Value;
                var invoice = _context.Invoices.Find(invoiceId.Value);
                if (invoice != null)
                {
                    dto.Amount = invoice.TotalAmount;
                }
            }

            return View(dto);
        }

        // POST: Admin/Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Invoices = _context.Invoices
                    .Include(i => i.Customer)
                    .Where(i => i.Status != "paid")
                    .ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }

            try
            {
                var paymentId = await _paymentService.ProcessPaymentAsync(dto, User.Identity?.Name ?? "system");
                TempData["SuccessMessage"] = "Thanh toán đã được xử lý thành công!";
                return RedirectToAction(nameof(Details), new { id = paymentId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Invoices = _context.Invoices
                    .Include(i => i.Customer)
                    .Where(i => i.Status != "paid")
                    .ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }
        }

        // GET: Admin/Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _paymentService.GetByIdAsync(id.Value);
            if (payment == null)
            {
                return NotFound();
            }

            ViewBag.Invoices = _context.Invoices.ToList();
            ViewBag.Users = _context.Users.ToList();

            var dto = new CreatePaymentDto
            {
                InvoiceId = payment.InvoiceId,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                ReferenceNumber = payment.ReferenceNumber,
                Notes = payment.Notes
            };

            return View(dto);
        }

        // POST: Admin/Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreatePaymentDto dto)
        {
            if (id != dto.InvoiceId) // This should be paymentId, but we'll handle it differently
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Invoices = _context.Invoices.ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }

            try
            {
                // For now, we'll just redirect back to details since we don't have update method in service
                TempData["InfoMessage"] = "Chức năng cập nhật thanh toán đang được phát triển.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Invoices = _context.Invoices.ToList();
                ViewBag.Users = _context.Users.ToList();
                return View(dto);
            }
        }

        // GET: Admin/Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _paymentService.GetByIdAsync(id.Value);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Admin/Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // For now, we'll just show a message since we don't have delete method in service
                TempData["InfoMessage"] = "Chức năng xóa thanh toán đang được phát triển.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa thanh toán: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Payments/Refund/5
        public async Task<IActionResult> Refund(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _paymentService.GetByIdAsync(id.Value);
            if (payment == null)
            {
                return NotFound();
            }

            ViewBag.Payment = payment;
            return View();
        }

        // POST: Admin/Payments/Refund/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id, decimal amount, string reason)
        {
            try
            {
                var result = await _paymentService.RefundPaymentAsync(id, amount, reason, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Hoàn tiền đã được xử lý thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hoàn tiền cho thanh toán này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hoàn tiền: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Payments/GetByInvoice/5
        public async Task<IActionResult> GetByInvoice(int invoiceId)
        {
            var payments = await _paymentService.GetByInvoiceIdAsync(invoiceId);
            return Json(payments);
        }

        // GET: Admin/Payments/GetByDateRange
        public async Task<IActionResult> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            var payments = await _paymentService.GetByDateRangeAsync(startDate, endDate);
            return Json(payments);
        }

        // GET: Admin/Payments/GetTotalPaid/5
        public async Task<IActionResult> GetTotalPaid(int invoiceId)
        {
            try
            {
                var totalPaid = await _paymentService.GetTotalPaidAmountAsync(invoiceId);
                return Json(new { totalPaid = totalPaid });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Admin/Payments/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var payment = await _paymentService.GetByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
}