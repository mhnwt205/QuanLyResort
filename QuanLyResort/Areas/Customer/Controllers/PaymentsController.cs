using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
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

        // GET: Customer/Payments
        public async Task<IActionResult> Index()
        {
            // In a real application, filter by logged-in customer's invoices
            // For now, we'll show all payments
            var payments = await _paymentService.GetAllAsync();
            return View(payments);
        }

        // GET: Customer/Payments/Details/5
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

        // GET: Customer/Payments/Create
        public IActionResult Create(int? invoiceId)
        {
            ViewBag.Invoices = _context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.Status != "paid")
                .ToList();

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

        // POST: Customer/Payments/Create
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
                return View(dto);
            }

            try
            {
                var paymentId = await _paymentService.ProcessPaymentAsync(dto, User.Identity?.Name ?? "customer");
                TempData["SuccessMessage"] = "Thanh toán đã được gửi thành công! Chúng tôi sẽ xác nhận và xử lý.";
                return RedirectToAction(nameof(Details), new { id = paymentId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Invoices = _context.Invoices
                    .Include(i => i.Customer)
                    .Where(i => i.Status != "paid")
                    .ToList();
                return View(dto);
            }
        }

        // GET: Customer/Payments/Print/5
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
