using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ResortDbContext _context;

        public DashboardController(ResortDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Thống kê tổng quan
                var totalRooms = await _context.Rooms.CountAsync();
                var bookedRooms = await _context.Bookings
                    .Where(b => b.Status == "Confirmed" || b.Status == "CheckedIn")
                    .CountAsync();
                var availableRooms = totalRooms - bookedRooms;

                // Doanh thu hôm nay
                var todayRevenue = await _context.Invoices
                    .Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value.Date == DateTime.Today)
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

                // Doanh thu tháng này
                var monthlyRevenue = await _context.Invoices
                    .Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value.Month == DateTime.Now.Month && i.CreatedAt.Value.Year == DateTime.Now.Year)
                    .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

                // Tổng số khách hàng
                var totalCustomers = await _context.Customers.CountAsync();

                // Tổng số đặt phòng
                var totalBookings = await _context.Bookings.CountAsync();

                // Đặt phòng đang chờ duyệt
                var pendingBookings = await _context.Bookings
                    .Where(b => b.Status == "Pending")
                    .CountAsync();

                // Phòng sắp check-out hôm nay
                var checkOutToday = await _context.Bookings
                    .Where(b => b.CheckOutDate == DateOnly.FromDateTime(DateTime.Today) && b.Status == "CheckedIn")
                    .CountAsync();

                ViewBag.TotalRooms = totalRooms;
                ViewBag.BookedRooms = bookedRooms;
                ViewBag.AvailableRooms = availableRooms;
                ViewBag.TodayRevenue = todayRevenue;
                ViewBag.MonthlyRevenue = monthlyRevenue;
                ViewBag.TotalCustomers = totalCustomers;
                ViewBag.TotalBookings = totalBookings;
                ViewBag.PendingBookings = pendingBookings;
                ViewBag.CheckOutToday = checkOutToday;

                // Lấy danh sách đặt phòng gần đây
                var recentBookings = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewBag.RecentBookings = recentBookings;

                return View();
            }
            catch (Exception ex)
            {
                // Log error
                ViewBag.Error = "Có lỗi xảy ra khi tải dữ liệu dashboard: " + ex.Message;
                return View();
            }
        }
    }
}
