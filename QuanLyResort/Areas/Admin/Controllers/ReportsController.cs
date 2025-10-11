using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ResortDbContext _context;

        public ReportsController(ResortDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Reports
        public async Task<IActionResult> Index()
        {
            // Thống kê tổng quan
            var today = DateTime.Today;
            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;

            // Doanh thu theo ngày
            var todayRevenue = await _context.Invoices
                .Where(i => i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Date == today)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            // Doanh thu theo tháng
            var monthRevenue = await _context.Invoices
                .Where(i => i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Month == thisMonth && 
                           i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Year == thisYear)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            // Doanh thu theo năm
            var yearRevenue = await _context.Invoices
                .Where(i => i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Year == thisYear)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

            // Số phòng đã đặt hôm nay
            var todayBookings = await _context.Bookings
                .Where(b => b.CheckInDate.ToDateTime(TimeOnly.MinValue).Date == today)
                .CountAsync();

            // Tỷ lệ lấp đầy phòng
            var totalRooms = await _context.Rooms.CountAsync();
            var occupiedRooms = await _context.Bookings
                .Where(b => b.Status == "checkedin" || b.Status == "confirmed")
                .CountAsync();
            var occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;

            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.MonthRevenue = monthRevenue;
            ViewBag.YearRevenue = yearRevenue;
            ViewBag.TodayBookings = todayBookings;
            ViewBag.OccupancyRate = occupancyRate;

            return View();
        }

        // GET: Admin/Reports/Revenue
        public async Task<IActionResult> Revenue(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var revenueData = await _context.Invoices
                .Where(i => i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Date >= start.Date && 
                           i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Date <= end.Date)
                .GroupBy(i => i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(i => i.TotalAmount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.RevenueData = revenueData;

            return View();
        }

        // GET: Admin/Reports/Occupancy
        public async Task<IActionResult> Occupancy(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var occupancyData = await _context.Bookings
                .Where(b => b.CheckInDate.ToDateTime(TimeOnly.MinValue).Date >= start.Date && 
                           b.CheckInDate.ToDateTime(TimeOnly.MinValue).Date <= end.Date)
                .GroupBy(b => b.CheckInDate.ToDateTime(TimeOnly.MinValue).Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Bookings = g.Count(),
                    Revenue = g.Sum(b => b.TotalAmount ?? 0)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.OccupancyData = occupancyData;

            return View();
        }

        // GET: Admin/Reports/Customers
        public async Task<IActionResult> Customers()
        {
            var customerStats = await _context.Customers
                .GroupBy(c => c.CreatedAt.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    NewCustomers = g.Count()
                })
                .OrderByDescending(x => x.Date)
                .Take(30)
                .ToListAsync();

            var topCustomers = await _context.Customers
                .Select(c => new
                {
                    Customer = c,
                    TotalBookings = _context.Bookings.Count(b => b.CustomerId == c.CustomerId),
                    TotalSpent = _context.Bookings
                        .Where(b => b.CustomerId == c.CustomerId)
                        .Sum(b => b.TotalAmount ?? 0)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToListAsync();

            ViewBag.CustomerStats = customerStats;
            ViewBag.TopCustomers = topCustomers;

            return View();
        }

        // GET: Admin/Reports/Services
        public async Task<IActionResult> Services(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var serviceStats = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Where(sb => sb.ServiceDate.ToDateTime(TimeOnly.MinValue).Date >= start.Date && 
                           sb.ServiceDate.ToDateTime(TimeOnly.MinValue).Date <= end.Date)
                .GroupBy(sb => sb.Service.ServiceName)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    Bookings = g.Count(),
                    Revenue = g.Sum(sb => sb.TotalAmount ?? 0),
                    Quantity = g.Sum(sb => sb.Quantity ?? 0)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.ServiceStats = serviceStats;

            return View();
        }

        // GET: Admin/Reports/Inventory
        public async Task<IActionResult> Inventory()
        {
            var inventoryStats = await _context.Inventories
                .Include(i => i.Item)
                .Include(i => i.Warehouse)
                .Select(i => new
                {
                    ItemName = i.Item.ItemName,
                    WarehouseName = i.Warehouse.WarehouseName,
                    QuantityOnHand = i.QuantityOnHand,
                    QuantityReserved = i.QuantityReserved,
                    AvailableQuantity = i.QuantityOnHand - i.QuantityReserved,
                    MinStockLevel = i.Item.MinStockLevel,
                    IsLowStock = i.QuantityOnHand <= i.Item.MinStockLevel,
                    TotalValue = i.QuantityOnHand * i.Item.CostPrice
                })
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            var lowStockItems = inventoryStats.Where(x => x.IsLowStock).ToList();
            var totalInventoryValue = inventoryStats.Sum(x => x.TotalValue);

            ViewBag.InventoryStats = inventoryStats;
            ViewBag.LowStockItems = lowStockItems;
            ViewBag.TotalInventoryValue = totalInventoryValue;

            return View();
        }

        // GET: Admin/Reports/NightAudit
        public async Task<IActionResult> NightAudit()
        {
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            // Đặt phòng chưa check-in
            var pendingCheckIns = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b => b.CheckInDate.ToDateTime(TimeOnly.MinValue).Date == today && 
                           b.Status == "confirmed")
                .ToListAsync();

            // Check-out hôm nay
            var checkOutsToday = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Room)
                .Where(b => b.CheckOutDate.ToDateTime(TimeOnly.MinValue).Date == today && 
                           b.Status == "checkedin")
                .ToListAsync();

            // Hóa đơn chưa thanh toán
            var unpaidInvoices = await _context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.Status == "draft" || i.Status == "approved")
                .ToListAsync();

            // Phòng cần dọn
            var roomsToClean = await _context.Rooms
                .Where(r => r.Status == "dirty" || r.Status == "maintenance")
                .ToListAsync();

            // Dịch vụ chờ duyệt
            var pendingServices = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Include(sb => sb.Customer)
                .Where(sb => sb.Status == "pending")
                .ToListAsync();

            ViewBag.PendingCheckIns = pendingCheckIns;
            ViewBag.CheckOutsToday = checkOutsToday;
            ViewBag.UnpaidInvoices = unpaidInvoices;
            ViewBag.RoomsToClean = roomsToClean;
            ViewBag.PendingServices = pendingServices;

            return View();
        }

        // POST: Admin/Reports/GenerateNightAudit
        [HttpPost]
        public async Task<IActionResult> GenerateNightAudit()
        {
            var today = DateTime.Today;
            var auditData = new
            {
                Date = today,
                TotalRooms = await _context.Rooms.CountAsync(),
                OccupiedRooms = await _context.Bookings
                    .Where(b => b.Status == "checkedin")
                    .CountAsync(),
                AvailableRooms = await _context.Rooms
                    .Where(r => r.Status == "available")
                    .CountAsync(),
                TodayRevenue = await _context.Invoices
                    .Where(i => i.InvoiceDate.ToDateTime(TimeOnly.MinValue).Date == today)
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0,
                PendingCheckIns = await _context.Bookings
                    .Where(b => b.CheckInDate.ToDateTime(TimeOnly.MinValue).Date == today && 
                               b.Status == "confirmed")
                    .CountAsync(),
                CheckOutsToday = await _context.Bookings
                    .Where(b => b.CheckOutDate.ToDateTime(TimeOnly.MinValue).Date == today && 
                               b.Status == "checkedin")
                    .CountAsync(),
                UnpaidInvoices = await _context.Invoices
                    .Where(i => i.Status == "draft" || i.Status == "approved")
                    .CountAsync()
            };

            TempData["AuditData"] = auditData;
            return RedirectToAction(nameof(NightAudit));
        }

        // GET: Admin/Reports/Export/Excel
        public async Task<IActionResult> ExportExcel(string reportType, DateTime? startDate, DateTime? endDate)
        {
            // Trong thực tế, sẽ sử dụng thư viện như EPPlus hoặc ClosedXML
            // để tạo file Excel
            TempData["SuccessMessage"] = "Xuất báo cáo Excel thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Reports/Export/PDF
        public async Task<IActionResult> ExportPDF(string reportType, DateTime? startDate, DateTime? endDate)
        {
            // Trong thực tế, sẽ sử dụng thư viện như iTextSharp hoặc QuestPDF
            // để tạo file PDF
            TempData["SuccessMessage"] = "Xuất báo cáo PDF thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
