using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;
using QuanLyResort.Services.Interfaces;
using QuanLyResort.ViewModels;

namespace QuanLyResort.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ResortDbContext _context;

        public InvoicesController(IInvoiceService invoiceService, ResortDbContext context)
        {
            _invoiceService = invoiceService;
            _context = context;
        }

        // GET: Customer/Invoices
        public async Task<IActionResult> Index()
        {
            // In a real application, filter by logged-in customer
            // For now, we'll show all invoices
            var invoices = await _invoiceService.GetAllAsync();
            return View(invoices);
        }

        // GET: Customer/Invoices/Details/5
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

        // GET: Customer/Invoices/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _invoiceService.GetByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Customer/Invoices/GetByCustomer/5
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var invoices = await _invoiceService.GetByCustomerIdAsync(customerId);
            return Json(invoices);
        }
    }
}
