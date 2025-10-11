using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class WarehouseController : Controller
    {
        private readonly ResortDbContext _context;

        public WarehouseController(ResortDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Warehouse
        public async Task<IActionResult> Index()
        {
            var warehouses = await _context.Warehouses
                .Include(w => w.Manager)
                .ToListAsync();
            return View(warehouses);
        }

        // GET: Admin/Warehouse/Inventory
        public async Task<IActionResult> Inventory()
        {
            var inventories = await _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Warehouse)
                .OrderBy(i => i.Item.ItemName)
                .ToListAsync();
            return View(inventories);
        }

        // GET: Admin/Warehouse/Items
        public async Task<IActionResult> Items()
        {
            var items = await _context.Items
                .OrderBy(i => i.ItemName)
                .ToListAsync();
            return View(items);
        }

        // GET: Admin/Warehouse/Items/Details/5
        public async Task<IActionResult> ItemDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            // Lấy tồn kho theo kho
            var inventories = await _context.Inventories
                .Include(i => i.Warehouse)
                .Where(i => i.ItemId == id)
                .ToListAsync();

            ViewBag.Inventories = inventories;

            return View(item);
        }

        // GET: Admin/Warehouse/Items/Create
        public IActionResult CreateItem()
        {
            return View();
        }

        // POST: Admin/Warehouse/Items/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem([Bind("ItemId,ItemCode,ItemName,Description,CostPrice,SellingPrice,Unit,MinStockLevel,IsActive")] Item item)
        {
            if (ModelState.IsValid)
            {
                item.CreatedAt = DateTime.Now;
                item.UpdatedAt = DateTime.Now;
                _context.Add(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm vật tư thành công!";
                return RedirectToAction(nameof(Items));
            }
            return View(item);
        }

        // GET: Admin/Warehouse/Items/Edit/5
        public async Task<IActionResult> EditItem(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Admin/Warehouse/Items/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(int id, [Bind("ItemId,ItemCode,ItemName,Description,CostPrice,SellingPrice,Unit,MinStockLevel,IsActive")] Item item)
        {
            if (id != item.ItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    item.UpdatedAt = DateTime.Now;
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật vật tư thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.ItemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Items));
            }
            return View(item);
        }

        // GET: Admin/Warehouse/StockIssues
        public async Task<IActionResult> StockIssues()
        {
            var stockIssues = await _context.StockIssues
                .Include(si => si.Warehouse)
                .Include(si => si.Department)
                .Include(si => si.RequestedByNavigation)
                .Include(si => si.IssuedByNavigation)
                .OrderByDescending(si => si.CreatedAt)
                .ToListAsync();
            return View(stockIssues);
        }

        // GET: Admin/Warehouse/StockIssues/Details/5
        public async Task<IActionResult> StockIssueDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockIssue = await _context.StockIssues
                .Include(si => si.Warehouse)
                .Include(si => si.Department)
                .Include(si => si.RequestedByNavigation)
                .Include(si => si.IssuedByNavigation)
                .Include(si => si.ApprovedByNavigation)
                .FirstOrDefaultAsync(m => m.IssueId == id);

            if (stockIssue == null)
            {
                return NotFound();
            }

            return View(stockIssue);
        }

        // GET: Admin/Warehouse/StockIssues/Create
        public async Task<IActionResult> CreateStockIssue()
        {
            ViewData["WarehouseId"] = await _context.Warehouses.ToListAsync();
            ViewData["DepartmentId"] = await _context.Departments.ToListAsync();
            ViewData["RequestedBy"] = await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.IsActive == true)
                .ToListAsync();
            return View();
        }

        // POST: Admin/Warehouse/StockIssues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStockIssue([Bind("IssueId,WarehouseId,DepartmentId,RequestedBy,IssueNumber,Purpose,Status")] StockIssue stockIssue)
        {
            if (ModelState.IsValid)
            {
                stockIssue.IssueNumber = await GenerateStockIssueNumber();
                stockIssue.CreatedAt = DateTime.Now;
                stockIssue.UpdatedAt = DateTime.Now;
                _context.Add(stockIssue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo phiếu xuất kho thành công!";
                return RedirectToAction(nameof(StockIssues));
            }
            ViewData["WarehouseId"] = await _context.Warehouses.ToListAsync();
            ViewData["DepartmentId"] = await _context.Departments.ToListAsync();
            ViewData["RequestedBy"] = await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.IsActive == true)
                .ToListAsync();
            return View(stockIssue);
        }

        // POST: Admin/Warehouse/StockIssues/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStockIssue(int id)
        {
            var stockIssue = await _context.StockIssues.FindAsync(id);
            if (stockIssue != null)
            {
                stockIssue.Status = "approved";
                stockIssue.UpdatedAt = DateTime.Now;
                _context.Update(stockIssue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Duyệt phiếu xuất kho thành công!";
            }
            return RedirectToAction(nameof(StockIssues));
        }

        // POST: Admin/Warehouse/StockIssues/Issue/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueStock(int id)
        {
            var stockIssue = await _context.StockIssues.FindAsync(id);
            if (stockIssue != null)
            {
                stockIssue.Status = "issued";
                stockIssue.UpdatedAt = DateTime.Now;
                _context.Update(stockIssue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xuất kho thành công!";
            }
            return RedirectToAction(nameof(StockIssues));
        }

        // GET: Admin/Warehouse/Reports
        public async Task<IActionResult> Reports()
        {
            // Thống kê tổng quan kho
            var totalItems = await _context.Items.CountAsync();
            var lowStockItems = await _context.Inventories
                .Include(i => i.Item)
                .Where(i => i.QuantityOnHand <= i.Item.MinStockLevel)
                .CountAsync();
            var totalStockValue = await _context.Inventories
                .Include(i => i.Item)
                .SumAsync(i => i.QuantityOnHand * i.Item.CostPrice);

            ViewBag.TotalItems = totalItems;
            ViewBag.LowStockItems = lowStockItems;
            ViewBag.TotalStockValue = totalStockValue;

            // Danh sách vật tư sắp hết
            var lowStockList = await _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Warehouse)
                .Where(i => i.QuantityOnHand <= i.Item.MinStockLevel)
                .ToListAsync();

            ViewBag.LowStockList = lowStockList;

            return View();
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.ItemId == id);
        }

        private async Task<string> GenerateStockIssueNumber()
        {
            var today = DateTime.Today;
            var prefix = $"SI{today:yyyyMMdd}";
            var lastIssue = await _context.StockIssues
                .Where(si => si.IssueNumber.StartsWith(prefix))
                .OrderByDescending(si => si.IssueNumber)
                .FirstOrDefaultAsync();

            if (lastIssue == null)
            {
                return $"{prefix}001";
            }

            var lastNumber = int.Parse(lastIssue.IssueNumber.Substring(prefix.Length));
            return $"{prefix}{(lastNumber + 1):D3}";
        }
    }
}
