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
    public class StockIssuesController : Controller
    {
        private readonly IStockIssueService _stockIssueService;
        private readonly ResortDbContext _context;

        public StockIssuesController(IStockIssueService stockIssueService, ResortDbContext context)
        {
            _stockIssueService = stockIssueService;
            _context = context;
        }

        // GET: Admin/StockIssues
        public async Task<IActionResult> Index()
        {
            var stockIssues = await _stockIssueService.GetAllAsync();
            return View(stockIssues);
        }

        // GET: Admin/StockIssues/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockIssue = await _stockIssueService.GetByIdAsync(id.Value);
            if (stockIssue == null)
            {
                return NotFound();
            }

            return View(stockIssue);
        }

        // GET: Admin/StockIssues/Create
        public IActionResult Create()
        {
            ViewBag.Warehouses = _context.Warehouses.ToList();
            ViewBag.Departments = _context.Departments.ToList();
            ViewBag.Items = _context.Items
                .Include(i => i.Inventories)
                .Where(i => i.IsActive == true)
                .ToList();

            return View(new CreateStockIssueDto());
        }

        // POST: Admin/StockIssues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStockIssueDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Warehouses = _context.Warehouses.ToList();
                ViewBag.Departments = _context.Departments.ToList();
                ViewBag.Items = _context.Items
                    .Include(i => i.Inventories)
                    .Where(i => i.IsActive == true)
                    .ToList();
                return View(dto);
            }

            try
            {
                var stockIssueId = await _stockIssueService.CreateAsync(dto, User.Identity?.Name ?? "system");
                TempData["SuccessMessage"] = "Phiếu xuất kho đã được tạo thành công!";
                return RedirectToAction(nameof(Details), new { id = stockIssueId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Warehouses = _context.Warehouses.ToList();
                ViewBag.Departments = _context.Departments.ToList();
                ViewBag.Items = _context.Items
                    .Include(i => i.Inventories)
                    .Where(i => i.IsActive == true)
                    .ToList();
                return View(dto);
            }
        }

        // GET: Admin/StockIssues/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockIssue = await _stockIssueService.GetByIdAsync(id.Value);
            if (stockIssue == null)
            {
                return NotFound();
            }

            ViewBag.Warehouses = _context.Warehouses.ToList();
            ViewBag.Departments = _context.Departments.ToList();
            ViewBag.Items = _context.Items
                .Include(i => i.Inventories)
                .Where(i => i.IsActive == true)
                .ToList();

            // For now, we'll just redirect to details since we don't have update method in service
            TempData["InfoMessage"] = "Chức năng cập nhật phiếu xuất kho đang được phát triển.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/StockIssues/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockIssue = await _stockIssueService.GetByIdAsync(id.Value);
            if (stockIssue == null)
            {
                return NotFound();
            }

            return View(stockIssue);
        }

        // POST: Admin/StockIssues/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _stockIssueService.CancelAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Phiếu xuất kho đã được hủy thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể hủy phiếu xuất kho này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hủy phiếu xuất kho: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/StockIssues/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var result = await _stockIssueService.ApproveAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Phiếu xuất kho đã được duyệt thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể duyệt phiếu xuất kho này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi duyệt phiếu xuất kho: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/StockIssues/Issue/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(int id)
        {
            try
            {
                var result = await _stockIssueService.IssueAsync(id, User.Identity?.Name ?? "system");
                if (result)
                {
                    TempData["SuccessMessage"] = "Xuất kho đã được thực hiện thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể thực hiện xuất kho này.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xuất kho: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/StockIssues/GetByDepartment/5
        public async Task<IActionResult> GetByDepartment(int departmentId)
        {
            var stockIssues = await _stockIssueService.GetByDepartmentIdAsync(departmentId);
            return Json(stockIssues);
        }

        // GET: Admin/StockIssues/GetByWarehouse/5
        public async Task<IActionResult> GetByWarehouse(int warehouseId)
        {
            var stockIssues = await _stockIssueService.GetByWarehouseIdAsync(warehouseId);
            return Json(stockIssues);
        }

        // GET: Admin/StockIssues/GetByStatus
        public async Task<IActionResult> GetByStatus(string status)
        {
            var stockIssues = await _stockIssueService.GetByStatusAsync(status);
            return Json(stockIssues);
        }

        // GET: Admin/StockIssues/ValidateStock
        [HttpGet]
        public async Task<IActionResult> ValidateStock(int itemId, int warehouseId, int quantity)
        {
            try
            {
                var isValid = await _stockIssueService.ValidateStockAvailabilityAsync(itemId, warehouseId, quantity);
                return Json(new { isValid = isValid });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: Admin/StockIssues/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var stockIssue = await _stockIssueService.GetByIdAsync(id);
            if (stockIssue == null)
            {
                return NotFound();
            }

            return View(stockIssue);
        }
    }
}
